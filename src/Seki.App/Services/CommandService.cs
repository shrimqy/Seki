using Seki.App.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Services
{
    public class CommandService
    {
        private static CommandService? _instance;
        public static CommandService Instance => _instance ??= new CommandService();


        public async Task HandleCommandMessageAsync(Command message)
        {

        }
    }
} 

