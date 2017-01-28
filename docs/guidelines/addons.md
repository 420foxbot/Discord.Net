# Addon Design Guidelines

>![WARN]
>This document is a work-in-progress!

These are guidelines that addon developers should adhere to before submitting 
their addon for display on the addon feed.

For the sake of consistency, even if your addon is not being submitted to our 
public feed, it is recommended to design your addons around these guidelines.

1. The root namespace of your addon should follow the [NuGet Naming Scheme]
2. Your addon's root namespace should be a member of `Discord.Addons`
	- For example, an addon named 'EmojiTools' would have the root namespace
	`Discord.Addons.EmojiTools`
3. Should your addon contain a module that depends on any services, it should
**not** accept an IDependencyMap as a constructor. All dependencies should be
named individually.
	```cs
	// Non-Example
	public GameModuleBase(IDependencyMap) { }
	// Example
	public GameModuleBase(IGameService gameService, IGameConfiguration config) { }
	```
4. Addons should provide an extension method that constructs any prerequisites
of the addon (e.g. service classes, TypeReaders)
	```cs
	public static Task UseGameService<T>(this CommandService commands, IDependencyMap map)
		where T : GameModuleBase
	{
		map.Add(new GameSerivce());
		commands.AddTypeReader<GameTypeReader>(new GameTypeReader());
		return Commands.AddModuleAsync<T>();
	}
	```
5. Addons should adhere to the service-module pattern, as defined in the bot
guidelines
6. Addons that create commands should **not** do so without exposing an
underlying service
	- For example, if you have an addon, `SimpleHelp`, it should provide both an
	extension method, `CommandService.UseHelpCommand`, and a service,
	`HelpService`. 
	- The addon's commands should not contain any logic; all logic should
	be placed in the service
7. An addon should accept an optional `Func<LogMessage, Task>` for logging. This
allows consumers of the addon to use their own logging framework, and have one
method for both Discord.Net's log output, and any addons.
8. The public API of an addon should be documented
9. Addons should not mutate anything on Discord without clear documentation
	- For example, an addon should not modify the permissions of a Text Channel
	unless it is clearly documented that this will happen.
10. Addons that communicate with an external API should not store any of the 
consumer's information (e.g. API keys) independently.
11. Additionally, addons should not communicate any private information related
to the bot, e.g. the bot's token
12. Addons should not attempt to circumvent ratelimits or otherwise abuse 
external APIs

[NuGet Naming Scheme]: https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package#choosing-a-unique-package-identifier-and-setting-the-version-number