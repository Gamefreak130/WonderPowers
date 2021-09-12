using Gamefreak130.Common.Loggers;
using Gamefreak130.WonderPowersSpace.Helpers;
using System;

namespace Gamefreak130.WonderPowersSpace.Loggers
{
    internal class PowerExceptionLogger : EventLogger<Exception>
    {
        private PowerExceptionLogger()
        {
        }

        internal static readonly PowerExceptionLogger sInstance = new();

        protected override string WriteNotification() => WonderPowerManager.LocalizeString("PowerError");
    }
}
