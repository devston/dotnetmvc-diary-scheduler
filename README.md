# ASP.Net Core MVC Diary Scheduler
An ASP.Net Core 6 MVC diary scheduling app based on the domain-driven design concept. This makes use of ASP.Net Core 6 MVC, WebPack and fullcalendar.

[![.NET](https://github.com/devston/dotnetmvc-diary-scheduler/actions/workflows/dotnet.yml/badge.svg)](https://github.com/devston/dotnetmvc-diary-scheduler/actions/workflows/dotnet.yml)

# Getting Started:
## Prerequisites
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with ASP.Net/ web component and .Net 6.0 SDK installed.
- [Node js](https://nodejs.org/en/)

## Running the solution
- Run `update-database` in the **nuget package manager** console against the `DiaryScheduler.Data` project to create the initial datastore.
- Start `DiaryScheduler.Presentation.Web`
- Create a user & navigate to the scheduler area

## TODO:
- Switch validation layer to use FluentValidation.
- Improve styling for account pages.
- Remove jQuery
- Add google calendar integration
- Add Office 365 integration

## Credits

- [fullcalendar](https://fullcalendar.io/)
- [WebPack](https://webpack.js.org/)
- [jQuery](https://jquery.com/)
- [Autofac](https://autofac.org/)
