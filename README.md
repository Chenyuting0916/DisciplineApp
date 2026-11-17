# Discipline App 🎯

A gamified productivity web application built with **Blazor Server** and **.NET 7**. This application helps users build self-discipline through a Pomodoro timer, daily task tracking, and a rewarding XP system, all integrated with your Google Calendar.

## ✨ Features

- **🍅 Pomodoro Timer**: Focus timer with customizable duration to track your deep work sessions.
- **📅 Google Integration**: Seamlessly syncs with **Google Calendar** and **Google Tasks** to display your daily schedule and to-dos directly on the dashboard.
- **🎮 Gamification System**:
  - Earn **XP** for completing tasks and finishing Pomodoro sessions.
  - Level up your character as you stay consistent.
  - **Leaderboard** to compete with other users (or yourself!).
- **📊 Activity Heatmap**: GitHub-style contribution graph to visualize your daily productivity streaks.
- **🌍 Multi-language Support**: Fully localized in English, Traditional Chinese (繁體中文), and Japanese (日本語).
- **🌓 Modern UI**: Sleek, dark-themed responsive design.

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core Blazor Server (.NET 7)
- **Database**: SQLite (with Entity Framework Core)
- **Authentication**: Google OAuth 2.0
- **Hosting**: Azure App Service (Windows)
- **Deployment**: GitHub Actions (CI/CD)

## 🚀 Getting Started

### Prerequisites
- .NET 7.0 SDK or later
- A Google Cloud Project with OAuth 2.0 credentials

### Installation

1.  **Clone the repository**
    ```bash
    git clone https://github.com/yourusername/DisciplineApp.git
    cd DisciplineApp
    ```

2.  **Configure Google Authentication**
    - Create a project in [Google Cloud Console](https://console.cloud.google.com/).
    - Enable **Google Calendar API** and **Google Tasks API**.
    - Create OAuth 2.0 credentials (Client ID and Client Secret).
    - Put the client id in `appsettings.json` if you want. Keep the client secret out of git.
    - Local secret:
      ```bash
      dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
      dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
      ```
    - If this secret was ever committed, revoke it in Google Cloud Console and create a new one. Git history still contains the old value.

3.  **Run the Application**
    ```bash
    dotnet run
    ```
    The app will automatically create the SQLite database and apply migrations on startup.

## ☁️ Deployment (Azure)

This project is configured for deployment to **Azure App Service** using GitHub Actions.

1.  Create an Azure Web App.
2.  Get the **Publish Profile** from Azure Portal.
3.  Add secrets to your GitHub Repository:
    - `AZURE_WEBAPP_NAME`: Your App Service name.
    - `AZURE_WEBAPP_PUBLISH_PROFILE`: The content of your publish profile.
4.  In the Azure App Service configuration, set `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret`. Do not put the secret in the repository.
5.  Manually trigger the **Azure Web App Deploy** workflow in the Actions tab.

## 📝 License

This project is licensed under the MIT License.
