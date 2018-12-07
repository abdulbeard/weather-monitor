# weather-monitor

This project is an Azure Function, with a timer trigger.

It queries the www.weather.gov api [found here](https://www.weather.gov/documentation/services-web-api/) to get information on weather forecasts.
It also uses [Google Charts Api](https://developers.google.com/chart/image/) to draw charts, and uses [SendGrid](https://sendgrid.com/) to send Emails to the subscribers.
It also uses [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/) to persist susbscribers information and forecast reports.

Check it out and let me know what you think!
