using MySql.Data.MySqlClient;
using Notification.Wpf;
using Quartz;
using Quartz.Impl;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Task_ManagerCP3.Models;
using Task_ManagerCP3.Properties;

namespace Task_ManagerCP3.Services
{
    public class NotificationManager
    {
        private Notification.Wpf.NotificationManager notificationManager;
        private IScheduler scheduler;

        public NotificationManager()
        {
            notificationManager = new Notification.Wpf.NotificationManager();
            InitializeScheduler().GetAwaiter().GetResult();
        }

        public async Task RefreshNotifications()
        {
            await scheduler.Clear();
            await ScheduleNotifications();
        }


        private async Task InitializeScheduler()
        {
            scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            await scheduler.Start();
            await ScheduleNotifications();
        }

        private async Task ScheduleNotifications()
        {
            var tasksWithNotifications = GetTasksWithNotifications();
            var now = DateTime.Now;

            foreach (var task in tasksWithNotifications)
            {
                if (task.Date.HasValue && task.NotificationTime.HasValue)
                {
                    var notificationTime = new DateTime(
                        task.Date.Value.Year,
                        task.Date.Value.Month,
                        task.Date.Value.Day,
                        task.NotificationTime.Value.Hour,
                        task.NotificationTime.Value.Minute,
                        task.NotificationTime.Value.Second);

                    TimeSpan timeDifference = task.Date.Value - now;
                    TimeSpan timeLeft = task.Date.Value - notificationTime;

                    string hourText = InputValidator.GetCorrectWordForm(timeLeft.Hours, "час", "часа", "часов");
                    string minuteText = InputValidator.GetCorrectWordForm(timeLeft.Minutes, "минута", "минуты", "минут");


                    var repeatInfo = GetRepeatInfoForTask(task.Id);


                    if (repeatInfo == null)
                    {
                        if (timeDifference >= TimeSpan.Zero)
                        {
                            var timeBeforeEventJobKey = new JobKey($"TimeBeforeEventJob_{task.Id}", "Notifications");
                            if (await scheduler.CheckExists(timeBeforeEventJobKey))
                            {
                                await scheduler.DeleteJob(timeBeforeEventJobKey);
                            }

                            var timeBeforeEventJobDetail = JobBuilder.Create<NotificationJob>()
                                .WithIdentity(timeBeforeEventJobKey)
                                .Build();

                            timeBeforeEventJobDetail.JobDataMap.Put("task", task);
                            timeBeforeEventJobDetail.JobDataMap.Put("notificationManager", this);
                            timeBeforeEventJobDetail.JobDataMap.Put("message", $"До события осталось: {timeLeft.Hours} {hourText} {timeLeft.Minutes} {minuteText}");

                            var timeBeforeEventTrigger = TriggerBuilder.Create()
                                .WithIdentity($"TimeBeforeEventTrigger_{task.Id}", "Notifications")
                                .StartAt(notificationTime)
                                .Build();

                            await scheduler.ScheduleJob(timeBeforeEventJobDetail, timeBeforeEventTrigger);
                        }

                        var eventTimeJobKey = new JobKey($"EventTimeJob_{task.Id}", "Notifications");
                        if (await scheduler.CheckExists(eventTimeJobKey))
                        {
                            await scheduler.DeleteJob(eventTimeJobKey);
                        }

                        var eventTimeJobDetail = JobBuilder.Create<NotificationJob>()
                            .WithIdentity(eventTimeJobKey)
                            .Build();

                        eventTimeJobDetail.JobDataMap.Put("task", task);
                        eventTimeJobDetail.JobDataMap.Put("notificationManager", this);
                        eventTimeJobDetail.JobDataMap.Put("message", $"Событие завершилось:\n{task.Date.Value:yyyy-MM-dd HH:mm}");

                        var eventTimeTrigger = TriggerBuilder.Create()
                            .WithIdentity($"EventTimeTrigger_{task.Id}", "Notifications")
                            .StartAt(task.Date.Value)
                            .Build();

                        await scheduler.ScheduleJob(eventTimeJobDetail, eventTimeTrigger);
                    }

                    if (repeatInfo != null)
                    {
                        ITrigger repeatTrigger = null;
                        DateTime scheduleTime = repeatInfo?.RepeatDateTime ?? task.Date.Value;
                        var notificationScheduleDateTime = new DateTime(
                             scheduleTime.Year,
                             scheduleTime.Month,
                             scheduleTime.Day,
                             task.NotificationTime.Value.Hour,
                             task.NotificationTime.Value.Minute,
                             task.NotificationTime.Value.Second);

                        switch (repeatInfo.RepeatType)
                        {
                            case "Ежедневно":
                                string dailyCronExpression = $"0 {task.NotificationTime.Value.Minute} {task.NotificationTime.Value.Hour} * * ?";
                                repeatTrigger = TriggerBuilder.Create()
                                    .WithIdentity($"DailyTrigger_{task.Id}", "Notifications")
                                    .WithCronSchedule(dailyCronExpression)
                                    .Build();
                                break;
                            case "Еженедельно":
                                int dayOfWeek = ((int)notificationTime.DayOfWeek) + 1;
                                string cronExpression = $"0 {notificationTime.Minute} {notificationTime.Hour} ? * {dayOfWeek}";
                                repeatTrigger = TriggerBuilder.Create()
                                    .WithIdentity($"WeeklyTrigger_{task.Id}", "Notifications")
                                    .WithCronSchedule(cronExpression)
                                    .Build();
                                break;
                            case "Дата":
                                var eventTimeJobDetail = JobBuilder.Create<NotificationJob>()
                                    .WithIdentity($"EventTimeJob_{task.Id}", "Notifications")
                                    .Build();

                                eventTimeJobDetail.JobDataMap.Put("task", task);
                                eventTimeJobDetail.JobDataMap.Put("notificationManager", this);
                                eventTimeJobDetail.JobDataMap.Put("message", $"Событие завершилось:\n{scheduleTime:yyyy-MM-dd HH:mm}");

                                var eventTimeTrigger = TriggerBuilder.Create()
                                    .WithIdentity($"EventTimeTrigger_{task.Id}", "Notifications")
                                    .StartAt(notificationScheduleDateTime)
                                    .Build();

                                await scheduler.ScheduleJob(eventTimeJobDetail, eventTimeTrigger);
                                break;
                        }
                        if (repeatTrigger != null)
                        {
                            var repeatJobKey = new JobKey($"RepeatJob_{task.Id}", "Notifications");
                            if (await scheduler.CheckExists(repeatJobKey))
                            {
                                await scheduler.DeleteJob(repeatJobKey);
                            }

                            var repeatJobDetail = JobBuilder.Create<NotificationJob>()
                                .WithIdentity(repeatJobKey)
                                .Build();

                            repeatJobDetail.JobDataMap.Put("task", task);
                            repeatJobDetail.JobDataMap.Put("notificationManager", this);
                            repeatJobDetail.JobDataMap.Put("message", $"До события осталось: {timeLeft.Hours} {hourText} {timeLeft.Minutes} {minuteText}");

                            await scheduler.ScheduleJob(repeatJobDetail, repeatTrigger);
                        }
                    }

                }
            }
        }

        public class NotificationJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                var task = (TaskItem)context.JobDetail.JobDataMap["task"];
                var message = (string)context.JobDetail.JobDataMap["message"];
                var notificationManager = (NotificationManager)context.JobDetail.JobDataMap["notificationManager"];
                notificationManager.ShowNotification(task, message);
                return Task.CompletedTask;
            }
        }

        private RepeatInfo GetRepeatInfoForTask(int taskId)
        {
            RepeatInfo repeatInfo = null;

            using (var conn = App.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT taskID, repeatType, repeatDateTime
                    FROM RepeatTasks
                    WHERE taskID = @taskId";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            repeatInfo = new RepeatInfo
                            {
                                TaskID = reader.GetInt32("taskID"),
                                RepeatType = reader.IsDBNull(reader.GetOrdinal("repeatType")) ? null : reader.GetString("repeatType"),
                                RepeatDateTime = reader.IsDBNull(reader.GetOrdinal("repeatDateTime")) ? (DateTime?)null : reader.GetDateTime("repeatDateTime")
                            };
                        }
                    }
                }
            }

            return repeatInfo;
        }



        private List<TaskItem> GetTasksWithNotifications()
        {
            var tasksWithNotifications = new List<TaskItem>();

            using (var conn = App.GetConnection())
            {
                conn.Open();
                int currentUserId = App.GetUserId(App.AuthToken, conn);
                string query = @"
                    SELECT t.id, t.title, t.dateTime, t.hasNotification, n.notificationTime
                    FROM Tasks t
                    INNER JOIN Notification n ON t.id = n.taskID
                    WHERE t.hasNotification = TRUE AND t.userID = @userID";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userID", currentUserId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var task = new TaskItem
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                Date = reader.IsDBNull(reader.GetOrdinal("dateTime")) ? null : (DateTime?)reader.GetDateTime("dateTime"),
                                HasNotification = reader.GetBoolean("hasNotification"),
                                NotificationTime = reader.IsDBNull(reader.GetOrdinal("notificationTime")) ? null : (DateTime?)reader.GetDateTime("notificationTime")
                            };
                            tasksWithNotifications.Add(task);
                        }
                    }
                }
            }

            return tasksWithNotifications;
        }

        private async Task UpdateTaskAndRemoveNotification(TaskItem task)
        {
            using (var conn = App.GetConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {

                        using (var updateTaskCmd = new MySqlCommand("UPDATE Tasks SET hasNotification = @hasNotification, isRepeat = @isRepeat WHERE id = @taskId", conn))
                        {
                            updateTaskCmd.Parameters.AddWithValue("@hasNotification", false);
                            updateTaskCmd.Parameters.AddWithValue("@isRepeat", false);
                            updateTaskCmd.Parameters.AddWithValue("@taskId", task.Id);
                            await updateTaskCmd.ExecuteNonQueryAsync();
                        }

                        using (var deleteNotificationCmd = new MySqlCommand("DELETE FROM Notification WHERE taskID = @taskId", conn))
                        {
                            deleteNotificationCmd.Parameters.AddWithValue("@taskId", task.Id);
                            await deleteNotificationCmd.ExecuteNonQueryAsync();
                        }

                        using (var deleteRepeatCmd = new MySqlCommand("DELETE FROM RepeatTasks WHERE taskID = @taskId", conn))
                        {
                            deleteRepeatCmd.Parameters.AddWithValue("@taskId", task.Id);
                            await deleteRepeatCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                }
            }
        }


        private void ShowNotification(TaskItem task, string message)
        {
            _ = Application.Current.Dispatcher.Invoke(async () =>
            {
                var content = new NotificationContent
                {
                    Title = task.Title,
                    Message = message,
                    Type = NotificationType.Notification,
                    TrimType = NotificationTextTrimType.Trim,
                    RowsCount = 3,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2a2a2a")),
                    Foreground = new SolidColorBrush(Colors.White),
                    Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/icons/tm_icon.ico"))
                };

                if (message.StartsWith("Событие завершилось"))
                {
                    await UpdateTaskAndRemoveNotification(task);
                }

                if (Settings.Default.IsNotificationEnabled)
                {
                    notificationManager.Show(content);
                }

                if (Settings.Default.IsNotificationSoundEnabled)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                }

            });
        }

    }
}