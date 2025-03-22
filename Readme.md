# 🚀 RootAlert - Real-time Exception Tracking for .NET  
[![NuGet Badge](https://img.shields.io/nuget/v/RootAlert.svg)](https://www.nuget.org/packages/RootAlert/)

RootAlert is a lightweight **real-time error tracking** and alerting library for .NET applications. It captures unhandled exceptions, batches them intelligently, and sends alerts to **Microsoft Teams** and **Slack**.

## 🔥 Features  
- 🛠 **Automatic exception handling** via middleware  
- 🚀 **Real-time alerts** with batching to prevent spam  
- 📡 **Supports Microsoft Teams (Adaptive Cards) & Slack (Blocks & Sections)**  
- ⏳ **Customizable batch interval using `TimeSpan`**  
- 📩 **Rich error logs including request details, headers, and stack traces**  
- 🔗 **Supports Redis and MSSQL for persistent storage**

---

## 📦 Installation  
RootAlert is available on **NuGet**. Install it using:

```sh
 dotnet add package RootAlert --version 0.1.5
```

Or via Package Manager:
```sh
 Install-Package RootAlert -Version 0.1.5
```

---

## ⚡ Quick Start  

### **1️⃣ Configure RootAlert in `Program.cs`**  
Add RootAlert to your services and configure it to send alerts to **Microsoft Teams** or **Slack**.

**If you do not configure a storage option, RootAlert will default to in-memory storage.**

```csharp
using RootAlert.Config;
using RootAlert.Extensions;
using RootAlert.Storage;

var builder = WebApplication.CreateBuilder(args);

var rootAlertOptions = new List<RootAlertOption>
{
    new RootAlertOption
    {
        AlertMethod = AlertType.Teams,
        WebhookUrl = "https://your-teams-webhook-url"
    },
    new RootAlertOption
    {
        AlertMethod = AlertType.Slack,
        WebhookUrl = "https://your-slack-webhook-url"
    }
};

var rootAlertSetting = new RootAlertSetting
{
    BatchInterval = TimeSpan.FromSeconds(20),
    RootAlertOptions = rootAlertOptions,
};

builder.Services.AddRootAlert(rootAlertSetting);

var app = builder.Build();

// ✅ Handle exceptions first
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ✅ Then, log errors with RootAlert
app.UseRootAlert();

app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.Run();
```

✅ **Now, RootAlert will automatically capture all unhandled exceptions!**  

---

## ⚡ Persistent Storage Options (Redis & MSSQL)

RootAlert now supports **separate libraries** for persistent storage:

- 🔹 **RootAlert.Redis** → Stores logs in **Redis**
- 🔹 **RootAlert.MSSQL** → Stores logs in **SQL Server**

### **🛠 RootAlert.Redis**

#### **Installation**
```sh
 dotnet add package RootAlert.Redis --version 0.1.0
```

#### **Implementation**
```csharp
using RootAlert.Redis;

var rootAlertSetting = new RootAlertSetting
{
    Storage = new RedisAlertStorage("127.0.0.1:6379"),
    BatchInterval = TimeSpan.FromSeconds(20),
    RootAlertOptions = rootAlertOptions,
};
```

### **🔹 Why Use Redis Storage?**
- ✅ Ensures logs persist even if the app restarts or the App Pool recycles.
- ✅ Allows centralized error tracking across multiple instances.
- ✅ Ideal for **distributed applications** that run across multiple servers.

---

### **🛠 RootAlert.MSSQL**

#### **Installation**
```sh
 dotnet add package RootAlert.MSSQL --version 0.1.0
```

#### **Implementation**
```csharp
using RootAlert.MSSQL;

var rootAlertSetting = new RootAlertSetting
{
    Storage = new SqlServerAlertStorage("Server=myServerAddress;Database=myDB;User Id=myUser;Password=myPassword;"),
    BatchInterval = TimeSpan.FromSeconds(20),
    RootAlertOptions = rootAlertOptions,
};
```

### **🔹 Why Use MSSQL Storage?**
- ✅ Stores logs in a structured database for easy querying.
- ✅ Ideal for applications that require long-term error tracking.
- ✅ Supports advanced analytics and monitoring tools.

---

## ⚠️ Important Notes  

### **❗ If an exception filter is added, RootAlert won't work.**  
**Reason:** Exception filters handle errors before middleware gets a chance to process them. Since RootAlert works as middleware, it will never see the exception if a filter catches it first.

### **✅ Solution: Ensure RootAlert is added after any existing exception-handling middleware.**  
If your application has a global exception-handling middleware, register RootAlert **after** it to ensure exceptions are logged correctly. Example:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>(); // Your existing middleware
app.UseRootAlert(); // Register RootAlert after the exception middleware
```

---
## 🏆 Microsoft Teams Integration  

RootAlert supports **Microsoft Teams** integration via:  
    1. **Incoming Webhooks (Connector)** – Simple and quick setup. (Will be deprecated)  
    2. **Microsoft Teams Workflow API** – Easier than Power Automate, with a built-in Webhook template.  


## **🔹 Option 1: Using an Incoming Webhook (Connector)**  
This method is the easiest way to receive error alerts in a Teams channel.
### **📌 Steps to Get a Teams Webhook URL**  
    1. Open **Microsoft Teams** and go to the desired channel.  
    2. Click **"…" (More options) → Connectors**.  
    3. Find **"Incoming Webhook"** and click **"Configure"**.  
    4. Name it **RootAlert Notifications** and click **Create**.  
    5. Copy the **Webhook URL** and use it in `RootAlertOptions`.
    5. Copy the **Webhook URL** and use it in your RootAlert configuration.  
---
## **🔹 Option 2: Using Microsoft Teams Workflow API (via Webhook Template)**  
This method is even easier than Power Automate and uses a built-in workflow to receive data via Webhook.
🎥 **Watch this video for a step-by-step guide:**  
[![Microsoft Teams Workflow API Setup](https://img.youtube.com/vi/jHTU_jUnswY/0.jpg)](https://www.youtube.com/watch?v=jHTU_jUnswY)  
🔗 **[YouTube Link: https://www.youtube.com/watch?v=jHTU_jUnswY](https://www.youtube.com/watch?v=jHTU_jUnswY)**  
### **📌 Steps to Configure Teams Workflow API**  
1. **Open Microsoft Teams and Go to Workflows**  
   - Click on **“…” (More options) → Workflows**.  --> Create 
2. **Select "Post to a channel when a webhook request is received" Template**  
   - Search for **"Post to a channel when a webhook request is received"** and select the **ready-made template**.  
   - Click **Next** to proceed.  
3. **Choose Team and Channel**  
   - Select the **Team** where you want to post alerts.  
   - Choose the **Channel** where notifications should appear.  
---

## 💬 Slack Integration  
RootAlert supports **Slack** using **Blocks & Sections** for structured messages.

### **🔹 How to Get a Slack Webhook URL**  
1. Go to **https://api.slack.com/apps** and create a new Slack App.  
2. Enable **Incoming Webhooks** under **Features**.  
3. Click **"Add New Webhook to Workspace"** and select a channel.  
4. Copy the **Webhook URL** and use it in `RootAlertOptions`.

---

## ⚙️ Configuration Options  
| Option          | Description                                                             |
| --------------- | ----------------------------------------------------------------------- |
| `AlertMethod`   | `Teams` or `Slack` (Choose alerting platform)                           |
| `WebhookUrl`    | Webhook URL for Teams or Slack                                          |
| `BatchInterval` | TimeSpan (e.g., `TimeSpan.FromSeconds(20)`)                             |
| `Storage`       | Supports Redis (`RedisAlertStorage`) or MSSQL (`SqlServerAlertStorage`) |

---

## 🚨 Example Error Alert  
RootAlert captures **rich error details** including **request details, headers, and stack traces**:

```
🆔 Error ID: abc123
⏳ Timestamp: 02/05/2025 4:02:41 AM
----------------------------------------------------
🌐 REQUEST DETAILS
🔗 URL: /weatherforecast
📡 HTTP Method: GET
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

