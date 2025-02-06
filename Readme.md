# 🚀 RootAlert - Real-time Exception Tracking for .NET  
[![NuGet Badge](https://img.shields.io/nuget/v/RootAlert.svg)](https://www.nuget.org/packages/RootAlert/)  
RootAlert is a lightweight **real-time error tracking** and alerting library for .NET applications. It captures unhandled exceptions, batches them intelligently, and sends alerts to **Microsoft Teams**, **Slack**, and **Email**.

## 🔥 Features  
- 🛠 **Automatic exception handling** via middleware  
- 🚀 **Real-time alerts** with batching to prevent spam  
- 💪 **Supports Microsoft Teams (Adaptive Cards), Slack (Blocks & Sections), and Email (SMTP)**  
- ⏳ **Customizable batch interval using `TimeSpan`**  
- 📩 **Rich error logs including request details, headers, and stack traces**  
- 🛢 **Multi-service alerting** (Send alerts to multiple services at once!)  

---

## 📚 Installation  
RootAlert is available on **NuGet**. Install it using:

```sh
 dotnet add package RootAlert --version 0.1.3
```

Or via Package Manager:
```sh
 Install-Package RootAlert -Version 0.1.3
```

---

## ⚡ Quick Start  

### **1️⃣ Configure RootAlert in `Program.cs`**  
Add RootAlert to your services and configure multiple alert destinations.

```csharp
using RootAlert.Config;
using RootAlert.Extensions;

var builder = WebApplication.CreateBuilder(args);

var rootAlertOptions = new List<RootAlertOptions>
{
    new RootAlertOptions
    {
        AlertMethod = AlertType.Teams,
        WebhookUrl = "https://your-teams-webhook-url",
        BatchInterval = TimeSpan.FromMinutes(1)
    },
    new RootAlertOptions
    {
        AlertMethod = AlertType.Slack,
        WebhookUrl = "https://your-slack-webhook-url",
        BatchInterval = TimeSpan.FromMinutes(1)
    }
};

builder.Services.AddRootAlert(rootAlertOptions);

var app = builder.Build();
app.UseRootAlert(); // Enable RootAlert middleware

app.Run();
```

✅ **Now, RootAlert will automatically capture all unhandled exceptions and send alerts to multiple services!**  

---

## 🏆 Microsoft Teams Integration  
RootAlert supports **Microsoft Teams** via **Adaptive Cards** for structured error logging.

### **🔹 How to Get a Teams Webhook URL**  
1. Open **Microsoft Teams** and go to the desired channel.  
2. Click **"..." (More options) → Connectors**.  
3. Find **"Incoming Webhook"** and click **"Configure"**.  
4. Name it **RootAlert Notifications** and click **Create**.  
5. Copy the **Webhook URL** and use it in `RootAlertOptions`.

### **🔹 Example Teams Alert (Adaptive Card)**  
RootAlert sends alerts as **rich Adaptive Cards**:

![Teams Adaptive Card](https://user-images.githubusercontent.com/example/teams-card.png)

---

## 💬 Slack Integration  
RootAlert supports **Slack** using **Blocks & Sections** for structured messages.

### **🔹 How to Get a Slack Webhook URL**  
1. Go to **https://api.slack.com/apps** and create a new Slack App.  
2. Enable **Incoming Webhooks** under **Features**.  
3. Click **"Add New Webhook to Workspace"** and select a channel.  
4. Copy the **Webhook URL** and use it in `RootAlertOptions`.

### **🔹 Example Slack Alert (Blocks & Sections)**  
RootAlert sends Slack messages in **a structured format**:

![Slack Alert](https://user-images.githubusercontent.com/example/slack-message.png)

---

## 🛠 Email Integration (Coming Soon)  
RootAlert will soon support **Email Alerts via SMTP**.

---

## ⚙️ Configuration Options  
| Option                        | Description                                             |
| ----------------------------- | ------------------------------------------------------- |
| `AlertMethod`                 | `Teams`, `Slack`, or `Email` (Choose alerting platform) |
| `WebhookUrl`                  | Webhook URL for Teams or Slack                          |
| `BatchInterval`               | TimeSpan (e.g., `TimeSpan.FromSeconds(30)`)             |
| `EmailSettings` (Coming Soon) | Configure SMTP for email alerts                         |

---

## 🚨 Example Error Alert  
RootAlert captures **rich error details** including **request details, headers, and stack traces**:

```
🄁 Error ID: abc123
🕛 Timestamp: 02/05/2025 4:02:41 AM
----------------------------------------------------
🌐 REQUEST DETAILS
🔗 URL: /weatherforecast
💼 HTTP Method: GET
----------------------------------------------------
📩 REQUEST HEADERS
📝 User-Agent: Mozilla/5.0
----------------------------------------------------
⚠️ EXCEPTION DETAILS
❗ Type: DivideByZeroException
💬 Message: Attempted to divide by zero.
----------------------------------------------------
🔍 STACK TRACE
   at Program.Main() in Program.cs:line 54
   at RootAlertMiddleware.Invoke()
----------------------------------------------------
```

---

## 🛠 Roadmap  
💡 **Database Storage** - Store logs in SQL, Redis, or NoSQL  
📝 **Email Alerts** - Send exception reports via SMTP  
🛠 **Log Severity Filtering** - Send only critical errors  

---

## 🐝 License  
RootAlert is open-source and available under the **MIT License**.

---

## 💡 Contributing  
🚀 Contributions are welcome! Feel free to submit pull requests or feature requests on [GitHub](https://github.com/satsvelke/RootAlert).  

---

## 🔗 Connect with Us  
📧 **Email:**  satsvelke@gmail.com  
🐦 **Twitter:** [@satsvelke](https://twitter.com/satsvelke)  

