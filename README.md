# Michaelsoft.Mailer
Simple .NETCore mailer with templates based on MailKit and MimeKit

You can build templates with **{{Key}}** markers and call SendEmailUsingTemplateAsync by passing a **Dictionary<string, string>**.
This mailer will do a Regex.Replace() on every dictionary key found in the template with the correspondent value.

You have to make a text template and an html template to make it work.

So simple to use as defining a folder for templates, registering the IEmailSender service providing the correct settings in the json env config file and using it.

**Note: This mailer is very simple, it's not suited for complex mailing systems because it has no performance tweak. Use it at your own risk. Open an issue if you need something and contribute. I will improve it when I will need more features.**
