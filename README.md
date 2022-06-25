# RinthBot

- Discord bot which main feature is to announce new updates of projects on [Modrinth](https://modrinth.com/)

## Example image:
*TBA*

## Invite
- You can invite the bot by clicking [here](https://discord.com/api/oauth2/authorize?client_id=986993814671614094&permissions=537316416&scope=bot%20applications.commands)

## Get Started
- For most commands you need to have an Administrator role
- After you invite the bot to your server, it's time to get setup
- To set the update channel, use `/modrinth set-update-channel` command, where as a parameter you provide the channel
    - **Make sure the bot has the permission to sent messages in the channel or to view the channel**
- Next up you can add projects you want to **subscribe** for updates
    - This can be done with the `/modrinth subscribe [projectID]`
    - Or you can use `/modrinth search [query]` to find the project you want to subscribe to and click the subscribe button
- Same way you can unsubscribe with `/modrinth unsubscribe [projectID]`
- To view the list of subscribed projects you can do `/modrinth list`
- With `/modrinth search [query]` you can search for Modrinth projects
- If you want to check that the bot can send messages to your update channel, do `/modrinth test-setup`

## TODOs
- [ ] Better UX overall
    - [x] Easy subscribe from search 
    - [x] Unsubscribe all projects command
    - [ ] Easier unsubscribing from list with buttons
    - [ ] Export/Import list of subscribed projects
    - [ ] Add config command which will have everything at hand
- [ ] Settings with different layouts of the update message
- [ ] Add ping role for updates
- [ ] Add option to have a custom role for managing subscribed projects
- [ ] Multiple channels support (so you can set per project channel)
- [ ] Support for other services? (e.g. CurseForge)

## Bug reports / Feature suggestions
- Be aware that this bot is still in **early development** and it may not be 100% stable
- If something's not working and the bot is online, check [Modrinth's status page](https://status.modrinth.com/) as Modrinth can be offline
- For bug reports and feature suggestions start a [new issue](https://github.com/Zechiax/RinthBot/issues/new)

## Data collected
- This bot needs to store some data so it can provide you with the updates
- Data stored are
    - Guild ID
    - Update Channel ID
    - List of subscribed projects
- Additional data may be added in the future (like planned layout options etc.)