import { Client as CommandClient } from "discord-slash-commands-client";
import { Client, Intents, WebhookClient } from "discord.js";
import fetch from "node-fetch";
import dotenv from "dotenv";
dotenv.config()

const GUILD_ID = ensureEnvExists("GUILD_ID");
const MODERATOR_ROLE_ID = ensureEnvExists("MODERATOR_ROLE_ID");
const BOT_TOKEN = ensureEnvExists("BOT_TOKEN");
const DISCORD_API = ensureEnvExists("DISCORD_API", "https://discord.com/api/v9");

const client = new Client({ intents: Intents.ALL });

client.on('ready', async () => {
    console.log(`Logged in as ${client.user.tag}!`);

    // https://www.npmjs.com/package/discord-slash-commands-client
    const commandClient = new CommandClient(BOT_TOKEN, client.user.id);

    // Search tracker
    addCommand({
        data: {
            name: "search",
            description: "Search for a player from Battlelog",
            options: [
                {
                    "name": "soldiername",
                    "description": "The name of the soldier",
                    "type": 3,
                    "required": true
                }
            ]
        }
    }, commandClient, [])

    // Add tracker
    addCommand({
        data: {
            name: "addtracker",
            description: "Start tracking a players joins",
            default_permission: false,
            options: [
                {
                    "name": "soldiername",
                    "description": "The name of the soldier",
                    "type": 3,
                    "required": true
                },
                {
                    "name": "reason",
                    "description": "The reason for adding the player in the tracked players list",
                    "type": 3,
                    "required": true
                },
            ]
        }
    }, commandClient, [{
        id: MODERATOR_ROLE_ID,
        type: 1,
        permission: true
    }])

    // Get trackers
    addCommand({
        data: {
            name: "gettrackers",
            description: "Get a list of tracked players",
            default_permission: false,
            options: [
                {
                    "name": "offset",
                    "description": "Offset for trackers. 25 trackers at a time is the maximum.",
                    "type": 4,
                    "required": false
                },
            ]
        }
    }, commandClient, [{
        id: MODERATOR_ROLE_ID,
        type: 1,
        permission: true
    }])

    // Remove tracker
    addCommand({
        data: {
            name: "removetracker",
            description: "Stop tracking a players joins",
            default_permission: false,
            options: [
                {
                    "name": "identifier",
                    "description": "The id of the tracker (tip: use /gettrackers to get the id)",
                    "type": 4,
                    "required": true
                },
            ]
        }
    }, commandClient, [{
        id: MODERATOR_ROLE_ID,
        type: 1,
        permission: true
    }])

    // commandClient.getCommands({ guildID: GUILD_ID })
    //     .then(commands => {
    //         commands.map(async command => {
    //             //console.log(command)
    //             await commandClient.editCommandPermissions([{
    //                 id: MODERATOR_ROLE_ID,
    //                 type: 1,
    //                 permission: true
    //             }], GUILD_ID, command.id).then(console.log).catch(console.error);
    //         })
    //     }).catch(console.error);

    // Print all command permissions
    //commandClient.getCommandPermissions(GUILD_ID).then(console.log).catch(console.error);

    client.ws.on('INTERACTION_CREATE', async interaction => {
        const command = interaction.data.name.toLowerCase();
        const args = interaction.data.options;

        switch (command) {
            case 'search': {
                // Reply with "BattleTracker is thinking..."
                await client.api.interactions(interaction.id, interaction.token).callback.post({
                    data: {
                        type: 5
                    }
                })

                // Post the data to backend so it actually adds the player in the tracked players list
                // Will also ack the "BattleTracker is thinking..." message with a proper message
                const soldierName = args.find(o => o.name === 'soldiername')?.value;
                const body = {
                    applicationId: interaction.application_id,
                    interactionToken: interaction.token,
                    soldierName: soldierName
                };

                fetch(`${process.env.BACKEND_ENDPOINT}/discord/searchSoldier/`, {
                    method: 'POST',
                    body: JSON.stringify(body),
                    headers: { 'Content-Type': 'application/json' },
                })
                    .catch(ex => {
                        console.error(ex)

                        deleteInteraction(interaction)
                    });
                break;
            }
            case 'addtracker': {
                // Reply with "BattleTracker is thinking..."
                await client.api.interactions(interaction.id, interaction.token).callback.post({
                    data: {
                        type: 5
                    }
                })

                // Post the data to backend so it actually adds the player in the tracked players list
                // Will also ack the "BattleTracker is thinking..." message with a proper message
                const soldierName = args.find(o => o.name === 'soldiername')?.value;
                const reason = args.find(o => o.name === 'reason')?.value;
                const body = {
                    applicationId: interaction.application_id,
                    interactionToken: interaction.token,
                    soldierName: soldierName,
                    reason: reason
                };

                fetch(`${process.env.BACKEND_ENDPOINT}/discord/addTracker/`, {
                    method: 'POST',
                    body: JSON.stringify(body),
                    headers: { 'Content-Type': 'application/json' },
                })
                    .catch(ex => {
                        console.error(ex)

                        deleteInteraction(interaction)
                    });
                break;
            }
            case 'gettrackers': {
                // Reply with "BattleTracker is thinking..."
                await client.api.interactions(interaction.id, interaction.token).callback.post({
                    data: {
                        type: 5
                    }
                })

                // Post the data to backend so it actually adds the player in the tracked players list
                // Will also ack the "BattleTracker is thinking..." message with a proper message
                const offset = args?.find(o => o.name === 'offset')?.value;
                const body = {
                    applicationId: interaction.application_id,
                    interactionToken: interaction.token,
                    offset: offset
                };

                fetch(`${process.env.BACKEND_ENDPOINT}/discord/getTrackers/`, {
                    method: 'POST',
                    body: JSON.stringify(body),
                    headers: { 'Content-Type': 'application/json' },
                })
                    .catch(ex => {
                        console.error(ex)

                        deleteInteraction(interaction)
                    });
                break;
            }
            case 'removetracker': {
                // Reply with "BattleTracker is thinking..."
                await client.api.interactions(interaction.id, interaction.token).callback.post({
                    data: {
                        type: 5
                    }
                })

                // Post the data to backend so it actually adds the player in the tracked players list
                // Will also ack the "BattleTracker is thinking..." message with a proper message
                const identifier = args.find(o => o.name === 'identifier')?.value;
                const body = {
                    applicationId: interaction.application_id,
                    interactionToken: interaction.token,
                    identifier: identifier,
                };

                fetch(`${process.env.BACKEND_ENDPOINT}/discord/removeTracker/`, {
                    method: 'DELETE',
                    body: JSON.stringify(body),
                    headers: { 'Content-Type': 'application/json' },
                })
                    .catch(ex => {
                        console.error(ex)

                        deleteInteraction(interaction)
                    });
                break;
            }
        }
    });
});

// Login to bot
client.login(BOT_TOKEN);

function ensureEnvExists(env, fallback) {
    if (env in process.env) {
        return process.env[env];
    } else if (fallback) {
        console.warn(`Environment variable "${env}" is not set. Using fallback "${fallback}"`);
        return fallback;
    } else {
        console.error(`Environment variable "${env}" is not set.`);
        process.exit(1);
    }
}

async function addCommand(commandData, commandClient, permissions) {
    const command = await client.api.applications(client.user.id).guilds(GUILD_ID).commands.post(commandData);

    if (command && command.id && commandClient && permissions) {
        await commandClient.editCommandPermissions(permissions, GUILD_ID, command.id).then(console.log).catch(console.error);
    }
}

async function deleteInteraction(interaction) {
    await fetch(`${DISCORD_API}/webhooks/${interaction.application_id}/${interaction.token}/messages/@original`, {
        method: 'DELETE'
    })
        .then(_ => console.log(`Interaction deleted from application ${interaction.application_id}`))
        .catch(ex => console.error(ex));
}