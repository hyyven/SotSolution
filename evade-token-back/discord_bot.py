import discord
import requests
import asyncio
import logging
import time
from discord.ext import tasks
from datetime import datetime, timedelta

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger("DiscordBot")

DISCORD_TOKEN = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXx"
ALERT_CHANNEL_ID = XXXXXXXXXXXXXXXXXXX
STATUS_CHANNEL_ID = XXXXXXXXXXXXXXXXXX
HEARTBEAT_URL = "http://localhost:6970/health"
OWNER_ID = XXXXXXXXXXXXXXXXXXXXXXXXXX
OWNER_ID2 = XXXXXXXXXXXXXXXXXXX
CHECK_INTERVAL = 1 * 60                 # 1 minute
TIMEOUT_THRESHOLD = 2 * 60              # 2 minutes
MESSAGE_REFRESH_INTERVAL = 15 * 60      # 15 minutes

class HeartbeatMonitor(discord.Client):
    def __init__(self):
        intents = discord.Intents.default()
        intents.message_content = True
        super().__init__(intents=intents)
        self.status_message = None
        self.last_successful_check = time.time()
        self.is_service_down = False
        self.down_notified = False
        self.start_time = time.time()
        self.last_status = "Starting"
        
    async def setup_hook(self):
        self.check_heartbeat.start()
        self.refresh_message.start()
        
    async def on_ready(self):
        logger.info(f'Logged in as {self.user} (ID: {self.user.id})')
        await self.change_presence(
            activity=discord.Activity(
                type=discord.ActivityType.watching, 
                name="service status"
            )
        )
        self.status_channel = self.get_channel(STATUS_CHANNEL_ID)
        self.alert_channel = self.get_channel(ALERT_CHANNEL_ID)
        if not self.status_channel:
            logger.error(f"Could not find status channel with ID {STATUS_CHANNEL_ID}")
            return
        if not self.alert_channel:
            logger.error(f"Could not find alert channel with ID {ALERT_CHANNEL_ID}")
            return
        embed = self.create_status_embed("Starting", "⚪")
        self.status_message = await self.status_channel.send(embed=embed)
        
    def create_status_embed(self, status, emoji):
        embed = discord.Embed(
            title=f"{emoji} Service Status", 
            description=f"Status: **{status}**", 
            color=0x00ff00 if status == "Online" else 0xff0000 if status == "Offline" else 0xffff00
        )
        embed.add_field(
            name="Last Check",
            value=f"<t:{int(time.time())}:R>",
            inline=True
        )
        if status != "Starting":
            if status != self.last_status:
                self.start_time = time.time()
                self.last_status = status
            embed.add_field(
                name="Status Since",
                value=f"<t:{int(self.start_time)}:R>",
                inline=True
            )
        embed.set_footer(text=f"Last updated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        return embed
        
    @tasks.loop(seconds=CHECK_INTERVAL)
    async def check_heartbeat(self):
        if not hasattr(self, 'status_channel') or not self.status_channel:
            return
        try:
            response = requests.get(HEARTBEAT_URL, timeout=10)
            if response.status_code == 200:
                data = response.json()
                self.last_successful_check = time.time()
                if data.get("status") == "error":
                    await self.handle_service_down("Service returned error status")
                elif self.is_service_down:
                    self.is_service_down = False
                    self.down_notified = False
                    logger.info("Service is back online")
                    embed = self.create_status_embed("Online", "🟢")
                    if self.status_message:
                        await self.status_message.edit(embed=embed)
                    else:
                        self.status_message = await self.status_channel.send(embed=embed)
                    await self.alert_channel.send("✅ **Service is back online!**")
                elif not self.status_message:
                    embed = self.create_status_embed("Online", "🟢")
                    self.status_message = await self.status_channel.send(embed=embed)
                else:
                    embed = self.create_status_embed("Online", "🟢")
                    await self.status_message.edit(embed=embed)
            else:
                await self.handle_service_down(f"Service returned status code {response.status_code}")
        except requests.RequestException as e:
            await self.handle_service_down(f"Could not connect to service: {str(e)}")
            
    @tasks.loop(seconds=MESSAGE_REFRESH_INTERVAL)
    async def refresh_message(self):
        if not hasattr(self, 'status_channel') or not self.status_channel:
            return
        if self.status_message:
            try:
                await self.status_message.delete()
                logger.info("Deleted old status message")
                status = "Online" if not self.is_service_down else "Offline"
                emoji = "🟢" if not self.is_service_down else "🔴"
                embed = self.create_status_embed(status, emoji)
                self.status_message = await self.status_channel.send(embed=embed)
                logger.info("Created new status message")
            except discord.errors.NotFound:
                logger.warning("Status message not found when trying to delete")
                status = "Online" if not self.is_service_down else "Offline"
                emoji = "🟢" if not self.is_service_down else "🔴"
                embed = self.create_status_embed(status, emoji)
                self.status_message = await self.status_channel.send(embed=embed)
            except Exception as e:
                logger.error(f"Error refreshing status message: {str(e)}")
                
    @check_heartbeat.before_loop
    async def before_check_heartbeat(self):
        await self.wait_until_ready()

    @refresh_message.before_loop
    async def before_refresh_message(self):
        await self.wait_until_ready()

    async def handle_service_down(self, reason):
        time_since_last_check = time.time() - self.last_successful_check
        if time_since_last_check > TIMEOUT_THRESHOLD or reason == "Service returned error status":
            self.is_service_down = True
            logger.warning(f"Service appears to be down: {reason}")
            embed = self.create_status_embed("Offline", "🔴")
            if self.status_message:
                await self.status_message.edit(embed=embed)
            else:
                self.status_message = await self.status_channel.send(embed=embed)
            if not self.down_notified:
                await self.alert_channel.send(
                    f"**ALERT: Server is down!** <@{OWNER_ID}>, <@{OWNER_ID2}>\n"
                    f"Last successful check: <t:{int(self.last_successful_check)}:R>\n"
                    f"Reason: {reason}"
                )
                self.down_notified = True
                
if __name__ == "__main__":
    client = HeartbeatMonitor()
    client.run(DISCORD_TOKEN)