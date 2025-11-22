---
description: How to obtain Google Client ID and Client Secret
---

# Setting up Google Authentication

To enable "Login with Google", you need to create credentials in the Google Cloud Console.

## 1. Create a Project
1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Click the project dropdown at the top and select **New Project**.
3. Name it "DisciplineApp" and click **Create**.

## 2. Configure OAuth Consent Screen
1. In the left sidebar, go to **APIs & Services** > **OAuth consent screen**.
2. Select **External** and click **Create**.
3. Fill in the required fields:
   - **App name**: DisciplineApp
   - **User support email**: Your email
   - **Developer contact information**: Your email
4. Click **Save and Continue** through the remaining steps (Scopes, Test Users).
   - *Note*: For development, add your own email as a **Test User**.

## 3. Create Credentials
1. Go to **APIs & Services** > **Credentials**.
2. Click **Create Credentials** > **OAuth client ID**.
3. Select **Web application**.
4. Name it "DisciplineApp Local".
5. Under **Authorized redirect URIs**, add:
   - `https://localhost:7223/signin-google`
   - `http://localhost:5070/signin-google`
   *(Check your `launchSettings.json` ports if different)*
6. Click **Create**.

## 4. Copy Keys
1. A dialog will appear with your **Client ID** and **Client Secret**.
2. Copy these values.
3. Open `appsettings.json` in your project.
4. Replace `YOUR_CLIENT_ID` and `YOUR_CLIENT_SECRET` with the copied values.

```json
"Authentication": {
  "Google": {
    "ClientId": "paste-client-id-here",
    "ClientSecret": "paste-client-secret-here"
  }
}
```

## 5. Enable Google Calendar API (Optional)
If you want to use the Calendar integration:
1. Go to **APIs & Services** > **Library**.
2. Search for "Google Calendar API".
3. Click **Enable**.
