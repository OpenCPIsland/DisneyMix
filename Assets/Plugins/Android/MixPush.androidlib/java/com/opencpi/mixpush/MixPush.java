package com.opencpi.mixpush;

import android.app.Activity;
import android.app.NotificationManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.service.notification.StatusBarNotification;
import android.util.Base64;

import java.security.SecureRandom;
import java.util.concurrent.ConcurrentLinkedQueue;

/**
 * What Unity calls (GlacierPushClient.cs). All methods are static and safe
 * to call from the Unity thread.
 */
public final class MixPush {
    static final String TAG = "MixPush";
    static final String PREFS = "mixpush";
    static final String KEY_TOKEN = "token";
    static final String KEY_URL = "url";
    static final String KEY_ENABLED = "enabled";
    static final String KEY_LAST_EVENT_ID = "lastEventId";

    /** The tapped notification's payload, on the intent that opens the app. */
    static final String EXTRA_PAYLOAD = "com.opencpi.mixpush.PAYLOAD";
    /** Tag on every message notification, so they can be cleared together. */
    static final String NOTIFICATION_TAG = "mixpush";

    private static final int INBOX_LIMIT = 50;
    private static final ConcurrentLinkedQueue<String> inbox = new ConcurrentLinkedQueue<>();

    /** True while the Unity activity is in front; pushes then go to the app, not the shade. */
    static volatile boolean appVisible;

    private MixPush() {}

    static SharedPreferences prefs(Context context) {
        return context.getApplicationContext().getSharedPreferences(PREFS, Context.MODE_PRIVATE);
    }

    /** This device's push token, made on first use. The server only knows it once Unity registers it. */
    public static String getToken(Context context) {
        SharedPreferences prefs = prefs(context);
        String token = prefs.getString(KEY_TOKEN, null);
        if (token == null) {
            byte[] bytes = new byte[24];
            new SecureRandom().nextBytes(bytes);
            token = Base64.encodeToString(bytes, Base64.URL_SAFE | Base64.NO_PADDING | Base64.NO_WRAP);
            prefs.edit().putString(KEY_TOKEN, token).remove(KEY_LAST_EVENT_ID).apply();
        }
        return token;
    }

    /** Forgets the token (on logout), so the next account gets a fresh one. */
    public static void resetToken(Context context) {
        prefs(context).edit().remove(KEY_TOKEN).remove(KEY_LAST_EVENT_ID).apply();
    }

    /** Starts (or keeps) the background connection to `baseUrl` (the Mix API host). */
    public static void start(Context context, String baseUrl) {
        prefs(context).edit().putString(KEY_URL, baseUrl).putBoolean(KEY_ENABLED, true).apply();
        MixPushService.start(context);
    }

    /** Stops the background connection; it stays stopped across reboots. */
    public static void stop(Context context) {
        prefs(context).edit().putBoolean(KEY_ENABLED, false).apply();
        context.stopService(new Intent(context, MixPushService.class));
    }

    public static boolean areNotificationsEnabled(Context context) {
        NotificationManager nm = context.getSystemService(NotificationManager.class);
        return nm != null && nm.areNotificationsEnabled();
    }

    /** Unity's OnApplicationPause. Opening the app clears the message notifications. */
    public static void setAppVisible(Context context, boolean visible) {
        appVisible = visible;
        if (visible) clearMessageNotifications(context);
    }

    /**
     * The next payload for Unity as JSON, or null. A tapped notification's
     * payload (on the activity's intent) comes first, then pushes that
     * arrived while the app was in front.
     */
    public static String getMessage(Activity activity) {
        if (activity != null) {
            Intent intent = activity.getIntent();
            if (intent != null && intent.hasExtra(EXTRA_PAYLOAD)) {
                String payload = intent.getStringExtra(EXTRA_PAYLOAD);
                intent.removeExtra(EXTRA_PAYLOAD);
                if (payload != null) return payload;
            }
        }
        return inbox.poll();
    }

    static void deliverToApp(String json) {
        inbox.add(json);
        while (inbox.size() > INBOX_LIMIT) inbox.poll();
    }

    static void clearMessageNotifications(Context context) {
        NotificationManager nm = context.getSystemService(NotificationManager.class);
        if (nm == null) return;
        for (StatusBarNotification n : nm.getActiveNotifications()) {
            if (NOTIFICATION_TAG.equals(n.getTag())) nm.cancel(n.getTag(), n.getId());
        }
    }
}
