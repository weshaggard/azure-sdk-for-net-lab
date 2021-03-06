﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Azure.Core.Http;
using Azure.Core.Http.Pipeline;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Testing
{
    public class TestEventListener : EventListener
    {
        const string SOURCE_NAME = "AzureSDK";

        public readonly List<string> Logged = new List<string>();
        EventLevel _enabled;
        EventSource _source;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
            if(eventSource.Name == SOURCE_NAME) {
                _source = eventSource;
                if(_enabled != default) {
                    EnableEvents(_source, _enabled);
                }
            }
        }

        public void EnableEvents(EventLevel level)
        {
            _enabled = level;
            if(_source != null) {
                EnableEvents(_source, _enabled);
            }

        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            base.OnEventWritten(eventData);
            if(eventData.EventSource.Name == SOURCE_NAME) {
                Logged.Add(eventData.EventName + " : " + eventData.Payload[0].ToString()); 
            }
        }

        public override string ToString()
            =>string.Join(" # ", Logged);
    }

    public class MockTransport : PipelineTransport
    {
        int[] _statusCodes;
        int _index;

        public MockTransport(params int[] statusCodes)
            => _statusCodes = statusCodes;

        public override HttpMessage CreateMessage(PipelineOptions options, CancellationToken cancellation)
            => new Message(ref options, cancellation);

        public override Task ProcessAsync(HttpMessage message)
        {
            var mockMessage = message as Message;
            if (mockMessage == null) throw new InvalidOperationException("the message is not compatible with the transport");

            mockMessage.SetStatus(_statusCodes[_index++]);
            if (_index >= _statusCodes.Length) _index = 0;
            return Task.CompletedTask;
        }

        class Message : HttpMessage
        {
            string _uri;
            int _status;
            PipelineMethod _method;

            protected override int Status => _status;

            protected override Stream ResponseContentStream => throw new NotImplementedException();

            public override PipelineMethod Method => throw new NotImplementedException();

            public Message(ref PipelineOptions client, CancellationToken cancellation)
                : base(cancellation)
            { }

            public void SetStatus(int status) => _status = status;

            public override void SetRequestLine(PipelineMethod method, Uri uri)
            {
                _uri = uri.ToString();
                _method = method;
            }

            public override string ToString()
                => $"{_method} {_uri}";

            protected override bool TryGetHeader(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
            {
                value = default;
                return false;
            }

            public override void AddHeader(HttpHeader header)
            {
            }

            public override void SetContent(PipelineContent content)
            {
                throw new NotImplementedException();
            }
        }
    }
}
