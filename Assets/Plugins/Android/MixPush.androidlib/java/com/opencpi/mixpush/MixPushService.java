package com.opencpi.mixpush;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.ServiceInfo;
import android.net.ConnectivityManager;
import android.net.Network;
import android.os.Build;
import android.os.IBinder;
import android.util.Log;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;

/**
 * Keeps GET {Mix API host}/push/stream open (Server-Sent Events, see
 * Glacier's services/mixPush.ts) and turns each event into a notification.
 *
 * Android only lets an app keep a connection while it's closed from a
 * foreground service, which must post a notification. Ours goes on its own
 * channel, separate from chats, created blocked (IMPORTANCE_NONE): Android
 * never shows it, and the service keeps running. Players can still turn the
 * channel on in the app's notification settings if they want to see it.
 */
public class MixPushService extends Service {
    /** Hidden. A new id: earlier builds made a visible "mixpush_connection", and an app can't hide an existing channel. */
    static final String CHANNEL_CONNECTION = "mixpush_connection_hidden";
    private static final String OLD_CHANNEL_CONNECTION = "mixpush_connection";
    static final String CHANNEL_MESSAGES = "mixpush_messages";
    private static final int FOREGROUND_ID = 0x4D495800;

    private static final int CONNECT_TIMEOUT_MS = 15_000;
    /** The server pings every 45 s; two missed pings mean the connection is dead. */
    private static final int READ_TIMEOUT_MS = 120_000;
    private static final long MIN_BACKOFF_MS = 5_000;
    private static final long MAX_BACKOFF_MS = 5 * 60_000;

    private enum Result { STOP, DROPPED, FAILED }

    private volatile boolean running;
    private volatile HttpURLConnection connection;
    private Thread worker;
    private final Object wakeup = new Object();
    private ConnectivityManager.NetworkCallback networkCallback;

    static void start(Context context) {
        try {
            context.startForegroundService(new Intent(context, MixPushService.class));
        } catch (RuntimeException e) {
            // ForegroundServiceStartNotAllowedException (Android 12+) when
            // started from the background; the next app launch starts it.
            Log.w(MixPush.TAG, "Couldn't start the push service: " + e);
        }
    }

    @Override
    public void onCreate() {
        super.onCreate();
        createChannels(this);
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        goForeground();
        SharedPreferences prefs = MixPush.prefs(this);
        if (!prefs.getBoolean(MixPush.KEY_ENABLED, false) || prefs.getString(MixPush.KEY_URL, null) == null
                || prefs.getString(MixPush.KEY_TOKEN, null) == null) {
            stopSelf();
            return START_NOT_STICKY;
        }
        if (worker == null) {
            running = true;
            worker = new Thread(this::run, "MixPush");
            worker.start();
            watchNetwork();
        }
        return START_STICKY;
    }

    @Override
    public void onDestroy() {
        running = false;
        HttpURLConnection c = connection;
        if (c != null) c.disconnect();
        if (worker != null) worker.interrupt();
        synchronized (wakeup) { wakeup.notifyAll(); }
        if (networkCallback != null) {
            try {
                getSystemService(ConnectivityManager.class).unregisterNetworkCallback(networkCallback);
            } catch (RuntimeException ignored) {}
        }
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    // -----------------------------------------------------------------------
    // Notifications

    static void createChannels(Context context) {
        NotificationManager nm = context.getSystemService(NotificationManager.class);
        NotificationChannel messages = new NotificationChannel(CHANNEL_MESSAGES, "Messages and friends", NotificationManager.IMPORTANCE_HIGH);
        messages.setDescription("New chat messages and friend requests");
        nm.createNotificationChannel(messages);
        NotificationChannel connection = new NotificationChannel(CHANNEL_CONNECTION, "Background connection", NotificationManager.IMPORTANCE_NONE);
        connection.setDescription("Keeps Mix connected so messages arrive. Hidden by default; messages arrive either way.");
        connection.setShowBadge(false);
        connection.setSound(null, null);
        connection.enableVibration(false);
        nm.createNotificationChannel(connection);
        nm.deleteNotificationChannel(OLD_CHANNEL_CONNECTION);
    }

    private void goForeground() {
        // Required by Android, but its channel is hidden (see createChannels).
        Notification notification = new Notification.Builder(this, CHANNEL_CONNECTION)
                .setSmallIcon(R.drawable.mixpush_ic_notification)
                .setContentTitle("Connected for new messages")
                .setCategory(Notification.CATEGORY_SERVICE)
                .setShowWhen(false)
                .setOngoing(true)
                .build();
        if (Build.VERSION.SDK_INT >= 34) {
            startForeground(FOREGROUND_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_REMOTE_MESSAGING);
        } else {
            startForeground(FOREGROUND_ID, notification);
        }
    }

    private void showNotification(JSONObject payload) {
        String type = payload.optString("type");
        String title = payload.optString("title", "Mix");
        String body = payload.optString("body", "");
        // One notification per chat (the latest message) or per friend event.
        String key = payload.has("chatThreadId") ? "chat:" + payload.optString("chatThreadId")
                : payload.has("friendshipInvitationId") ? "invite:" + payload.optString("friendshipInvitationId")
                : payload.has("friendId") ? "friend:" + payload.optString("friendId")
                : type;
        int id = key.hashCode();

        Intent open = getPackageManager().getLaunchIntentForPackage(getPackageName());
        if (open == null) return;
        try {
            JSONObject clicked = new JSONObject(payload.toString());
            clicked.put("inBackground", "true");
            clicked.put("Clicked", "true");
            open.putExtra(MixPush.EXTRA_PAYLOAD, clicked.toString());
        } catch (Exception ignored) {}
        open.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        PendingIntent tap = PendingIntent.getActivity(this, id, open, PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);

        // The lock screen shows only that something arrived.
        Notification publicVersion = new Notification.Builder(this, CHANNEL_MESSAGES)
                .setSmallIcon(R.drawable.mixpush_ic_notification)
                .setContentTitle(type.startsWith("FRIENDSHIP") ? "Friend update" : "New message")
                .build();
        Notification notification = new Notification.Builder(this, CHANNEL_MESSAGES)
                .setSmallIcon(R.drawable.mixpush_ic_notification)
                .setContentTitle(title)
                .setContentText(body)
                .setStyle(new Notification.BigTextStyle().bigText(body))
                .setContentIntent(tap)
                .setAutoCancel(true)
                .setCategory(type.startsWith("FRIENDSHIP") ? Notification.CATEGORY_SOCIAL : Notification.CATEGORY_MESSAGE)
                .setVisibility(Notification.VISIBILITY_PRIVATE)
                .setPublicVersion(publicVersion)
                .setWhen(System.currentTimeMillis())
                .setShowWhen(true)
                .build();
        // Dropped silently when the player hasn't allowed notifications.
        getSystemService(NotificationManager.class).notify(MixPush.NOTIFICATION_TAG, id, notification);
    }

    private void handlePush(String data) {
        JSONObject payload;
        try {
            payload = new JSONObject(data);
        } catch (Exception e) {
            Log.w(MixPush.TAG, "Ignoring a push that isn't JSON");
            return;
        }
        if (MixPush.appVisible) {
            // The app is open: hand it over (the SDK fetches the details) instead of the shade.
            try { payload.put("inBackground", "false"); } catch (Exception ignored) {}
            MixPush.deliverToApp(payload.toString());
        } else {
            showNotification(payload);
        }
    }

    // -----------------------------------------------------------------------
    // The connection

    private void run() {
        long backoff = MIN_BACKOFF_MS;
        while (running) {
            Result result;
            try {
                result = connectOnce();
            } catch (IOException e) {
                Log.i(MixPush.TAG, "Push connection failed: " + e.getMessage());
                result = Result.FAILED;
            } catch (RuntimeException e) {
                Log.w(MixPush.TAG, "Push connection error", e);
                result = Result.FAILED;
            }
            if (!running) return;
            if (result == Result.STOP) {
                // Push is off for this device (turned off, logged out, or
                // registered to another account): stay stopped.
                MixPush.prefs(this).edit().putBoolean(MixPush.KEY_ENABLED, false).apply();
                stopSelf();
                return;
            }
            if (result == Result.DROPPED) backoff = MIN_BACKOFF_MS;
            synchronized (wakeup) {
                try { wakeup.wait(backoff); } catch (InterruptedException e) { if (!running) return; }
            }
            backoff = Math.min(backoff * 2, MAX_BACKOFF_MS);
        }
    }

    private Result connectOnce() throws IOException {
        SharedPreferences prefs = MixPush.prefs(this);
        String base = prefs.getString(MixPush.KEY_URL, null);
        String token = prefs.getString(MixPush.KEY_TOKEN, null);
        if (base == null || token == null) return Result.STOP;
        while (base.endsWith("/")) base = base.substring(0, base.length() - 1);

        HttpURLConnection c = (HttpURLConnection) new URL(base + "/push/stream").openConnection();
        connection = c;
        try {
            c.setConnectTimeout(CONNECT_TIMEOUT_MS);
            c.setReadTimeout(READ_TIMEOUT_MS);
            c.setUseCaches(false);
            c.setRequestProperty("Accept", "text/event-stream");
            c.setRequestProperty("X-Mix-PushToken", token);
            long lastEventId = prefs.getLong(MixPush.KEY_LAST_EVENT_ID, 0);
            if (lastEventId > 0) c.setRequestProperty("Last-Event-ID", Long.toString(lastEventId));

            int code = c.getResponseCode();
            if (code == 410) return Result.STOP;
            if (code != 200) {
                Log.i(MixPush.TAG, "Push stream answered " + code);
                return Result.FAILED;
            }

            BufferedReader reader = new BufferedReader(new InputStreamReader(c.getInputStream(), StandardCharsets.UTF_8));
            String id = null;
            String event = "message";
            StringBuilder data = new StringBuilder();
            String line;
            while (running && (line = reader.readLine()) != null) {
                if (line.isEmpty()) {
                    if ("stop".equals(event)) return Result.STOP;
                    if (data.length() > 0) {
                        handlePush(data.toString());
                        if (id != null) {
                            try {
                                prefs.edit().putLong(MixPush.KEY_LAST_EVENT_ID, Long.parseLong(id)).apply();
                            } catch (NumberFormatException ignored) {}
                        }
                    }
                    id = null;
                    event = "message";
                    data.setLength(0);
                    continue;
                }
                if (line.startsWith(":")) continue; // comment / ping
                int colon = line.indexOf(':');
                String field = colon < 0 ? line : line.substring(0, colon);
                String value = colon < 0 ? "" : line.substring(colon + 1);
                if (value.startsWith(" ")) value = value.substring(1);
                switch (field) {
                    case "id": id = value; break;
                    case "event": event = value; break;
                    case "data":
                        if (data.length() > 0) data.append('\n');
                        data.append(value);
                        break;
                    default: break;
                }
            }
            return Result.DROPPED;
        } finally {
            connection = null;
            c.disconnect();
        }
    }

    /** Reconnects right away when a network comes back, instead of waiting out the backoff. */
    private void watchNetwork() {
        networkCallback = new ConnectivityManager.NetworkCallback() {
            @Override
            public void onAvailable(Network network) {
                synchronized (wakeup) { wakeup.notifyAll(); }
            }
        };
        try {
            getSystemService(ConnectivityManager.class).registerDefaultNetworkCallback(networkCallback);
        } catch (RuntimeException e) {
            networkCallback = null;
        }
    }
}
