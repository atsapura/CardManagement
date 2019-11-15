# Card Management
## Why?
This is a "real world" example application, written entirely in F#.
The goal is to create a best practice for building applications or at least give a reasonable manual to design one.
## Summary
It's a very simple bank application.

Here you can
- Create/Read users
- Create/Read cards for those users
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

For web api `Giraffe` framework is used. You can also play with it using `CardManagement.Console` project.

## Project overview

There are several projects in this solution, in order of referencing:

- CardManagement.Common. Self explanatory, I guess.
- CardManagement. This is a business logic project, a core of this application. Domain types, actions and composition root for this layer
- CardManagement.Data. Data access layer. Contains entities, db interaction functions and composition root for this layer.
- CardManagement.Infrastructure. In here you'll find a global composition root, logging, app configuration functions.
- CardManagement.Console/CardManagement.Api. Entry point for using global composition root from infrastructure.

## Detailed description

[Here's](https://github.com/atsapura/CardManagement/blob/master/article/Fighting%20complexity%20in%20software%20development.md) long and boring explanation of why anyone would even bother to use F# for web api and tips on how to do it in such a way so you don't blow your head off.