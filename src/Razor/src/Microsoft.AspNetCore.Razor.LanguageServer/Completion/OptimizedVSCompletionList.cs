﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    [JsonConverter(typeof(OptimizedVSCompletionListJsonConverter))]
    internal class OptimizedVSCompletionList : VSCompletionList
    {
        public OptimizedVSCompletionList(VSCompletionList completionList) : base(completionList)
        {
            CommitCharacters = completionList.CommitCharacters;
            Data = completionList.Data;
        }

        public class OptimizedVSCompletionListJsonConverter : OptimizedCompletionList.OptimizedCompletionListJsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(OptimizedVSCompletionList) == objectType;
            }

            protected override void WriteCompletionListProperties(JsonWriter writer, CompletionList completionList, JsonSerializer serializer)
            {
                var vsCompletionList = (OptimizedVSCompletionList)completionList;

                if (vsCompletionList.CommitCharacters != null)
                {
                    writer.WritePropertyName("_vs_commitCharacters");
                    serializer.Serialize(writer, vsCompletionList.CommitCharacters);
                }

                if (vsCompletionList.Data != null)
                {
                    writer.WritePropertyName("_vs_data");
                    serializer.Serialize(writer, vsCompletionList.Data);
                }

                base.WriteCompletionListProperties(writer, completionList, serializer);
            }
        }
    }
}
