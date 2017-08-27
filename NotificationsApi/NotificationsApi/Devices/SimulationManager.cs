using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationsApi.Devices
{
    public class SimulationManager
    {
        private CancellationTokenSource tokenSource;
        private CancellationToken token;

        private static SimulationManager _instance;

        public static SimulationManager Instance { get { return _instance ?? (_instance = new SimulationManager()); } }

        private void ResetSourceAndToken()
        {
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
        }

        public async Task SendAsync(string deviceId, string deviceKey)
        {
            
        }

        public void Stop()
        {
            tokenSource.Cancel();
        }
    }
}
