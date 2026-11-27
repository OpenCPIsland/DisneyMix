package com.opencpi.mixpush;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

/** Reconnects after a reboot or app update, if push was on. */
public class MixPushBootReceiver extends BroadcastReceiver {
    @Override
    public void onReceive(Context context, Intent intent) {
        String action = intent.getAction();
        if (!Intent.ACTION_BOOT_COMPLETED.equals(action) && !Intent.ACTION_MY_PACKAGE_REPLACED.equals(action)) return;
        if (MixPush.prefs(context).getBoolean(MixPush.KEY_ENABLED, false)) {
            MixPushService.start(context);
        }
    }
}
