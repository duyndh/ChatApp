﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CyDu.Model
{
    class MessageSend
    {
        [JsonProperty("type")]
        public long Type { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("conversationId")]
        public long ConversationId { get; set; }
    }
}
