# Card Management
## Why?
This is a "real world" example application, written entirely in F#.
The goal is to create a best practice for building applications or at least give a reasonable manual to design one.
## Summary
It's a very simple bank application.

Here you can
- Create/Update users
- Create/Update cards for those users
- Set daily limits for cards
- Top up balance
- Process payments (according to your current balance, daily limit and today's spendings)

## Tech
To run this thing you'll need:
- .NET Core 2.2+
- Docker
- Visual Studio 2017+ or VS Code with Ionide plugin or Rider with F# plugin

Database here is MongoDb, hosted in docker container.
So you just have to navigate to `docker` folder and run `docker-compose up`. That's it.

For web api `Giraffe` framework will be used. However now it's not there yet, so you have to run it from `CardManagement.Console` project for now.