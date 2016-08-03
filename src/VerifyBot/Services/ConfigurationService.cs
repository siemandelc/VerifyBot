using System;
using System.Collections.Generic;
using VerifyBot.Models;

namespace VerifyBot.Services
{
    public class ConfigurationService
    {
        public Configuration GetConfiguration()
        {
            var worldIDs = new List<int>();
            var worlds = Helper.SecretsReader.GetSecret("world_id");

            foreach (var world in worlds.Split(','))
            {
                int worldID = 0;
                if (!int.TryParse(world, out worldID))
                {
                    throw new Exception("Missing WorldID(s) field in configuration file");
                }

                worldIDs.Add(worldID);
            }

            var serverCandidate = Helper.SecretsReader.GetSecret("server_id");

            ulong serverID = 0;
            if (!ulong.TryParse(serverCandidate, out serverID))
            {
                throw new Exception("Missing ServerID Field in configuration file");
            }

            var verifyChannel = Helper.SecretsReader.GetSecret("verify_channel");

            if (verifyChannel == null)
            {
                throw new Exception("Missing verify_channel field in configuration file");
            }

            var verifyRole = Helper.SecretsReader.GetSecret("verify_role");

            if (verifyRole == null)
            {
                throw new Exception("Missing verify_role field in configuration file");
            }

            var adminChannel = Helper.SecretsReader.GetSecret("admin_channel");

            if (verifyChannel == null)
            {
                throw new Exception("Missing admin_channel field in configuration file");
            }

            var adminRole = Helper.SecretsReader.GetSecret("admin_role");

            if (verifyRole == null)
            {
                throw new Exception("Missing admin_role field in configuration file");
            }

            return new Configuration()
            {
                ServerID = serverID,
                WorldIDs = worldIDs,
                VerifyChannelName = verifyChannel,
                VerifyRole = verifyRole,
                AdminChannel = adminChannel,
                AdminRole = adminRole
            };
        }
    }
}