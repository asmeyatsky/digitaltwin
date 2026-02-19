import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import { Platform } from 'react-native';
import { registerDevice } from './api';

// Configure how notifications are handled when the app is in the foreground
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: true,
  }),
});

/**
 * Register for push notifications.
 * Requests permissions, gets an Expo push token, and registers it with the backend.
 * Safe to call multiple times — idempotent via backend upsert.
 */
export async function registerForPushNotifications(): Promise<string | null> {
  // Push notifications only work on physical devices
  if (!Device.isDevice) {
    console.log('Push notifications require a physical device');
    return null;
  }

  try {
    // Check existing permissions
    const { status: existingStatus } =
      await Notifications.getPermissionsAsync();
    let finalStatus = existingStatus;

    // Request permission if not already granted
    if (existingStatus !== 'granted') {
      const { status } = await Notifications.requestPermissionsAsync();
      finalStatus = status;
    }

    if (finalStatus !== 'granted') {
      console.log('Push notification permission not granted');
      return null;
    }

    // Get the Expo push token
    const tokenResponse = await Notifications.getExpoPushTokenAsync();
    const token = tokenResponse.data;

    // Determine the platform
    const platform =
      Platform.OS === 'ios'
        ? 'ios'
        : Platform.OS === 'android'
          ? 'android'
          : 'web';

    // Register the device token with the backend
    await registerDevice(token, platform);

    // Configure Android notification channel
    if (Platform.OS === 'android') {
      await Notifications.setNotificationChannelAsync('default', {
        name: 'Default',
        importance: Notifications.AndroidImportance.HIGH,
        vibrationPattern: [0, 250, 250, 250],
        lightColor: '#FF8A65',
      });
    }

    console.log('Push notifications registered successfully:', token);
    return token;
  } catch (error) {
    console.error('Failed to register for push notifications:', error);
    return null;
  }
}

/**
 * Handle a notification response (when the user taps on a notification).
 * Can be used to navigate to specific screens based on notification data.
 */
export function handleNotificationResponse(
  response: Notifications.NotificationResponse
): void {
  const data = response.notification.request.content.data;

  if (data?.type === 'checkin') {
    // The app could navigate to the check-in screen here
    console.log('User tapped check-in notification:', data.checkInType);
  }
}

/**
 * Set up listeners for incoming notifications and notification responses.
 * Returns a cleanup function to remove the listeners.
 */
export function setupNotificationListeners(): () => void {
  // Listen for notifications received while app is foregrounded
  const notificationListener =
    Notifications.addNotificationReceivedListener((notification) => {
      console.log('Notification received in foreground:', notification);
    });

  // Listen for user interactions with notifications
  const responseListener =
    Notifications.addNotificationResponseReceivedListener(
      handleNotificationResponse
    );

  return () => {
    Notifications.removeNotificationSubscription(notificationListener);
    Notifications.removeNotificationSubscription(responseListener);
  };
}
