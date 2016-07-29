# VerifyBot   

A bot that uses the Discord.NET and Guild Wars 2 API's to verify what world a users account is on. Made for Jade Quarry Discord but opened source for all to use.

You'll need a verified rank in Discord and a #verify text channel. When a user types !verify the bot will message them with instructions. Once a user is verified an entry is made into a SQLite database and the user is given the verified rank.

You'll need to add a secrets.txt file to the directory that the executable is running from. Here is the format.   
```
world_id: 1001,1008   
server_id: your_discord_server_id_here   
discord_token: your_discord_bot_token_here
```