﻿/*
 * Shadowsocks-Net https://github.com/shadowsocks/Shadowsocks-Net
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Argument.Check;
using Microsoft.Extensions.Logging;

namespace Shadowsocks.Cipher
{
    using Infrastructure;
    using Infrastructure.Pipe;
    using Infrastructure.Sockets;

    public class CipherUdpFilter : PipeFilter
    {
        IShadowsocksStreamCipher _cipher = null;
        public CipherUdpFilter(IClient tcpClient, IShadowsocksStreamCipher cipher, ILogger logger = null)
               : base(tcpClient, 10, logger)
        {
            _cipher = Throw.IfNull(() => cipher);
        }
        public override PipeFilterResult AfterReading(PipeFilterContext ctx)
        {
            PipeFilterResult r = new PipeFilterResult(this.Client, null, false);
            if (null != _cipher)
            {
                if (!ctx.Memory.IsEmpty)
                {
                    var bufferPlain = _cipher.DecryptUdp(ctx.Memory.Slice(0, ctx.MemoryLength));
                    if (null != bufferPlain && bufferPlain.SignificantLength > 0)
                    {
                        r = new PipeFilterResult(this.Client, bufferPlain, true);
                    }
                    else
                    {
                        _logger?.LogError($"AeadCipherTcpFilter AfterReading no plain data.");
                    }
                }
                else
                {
                    _logger?.LogError($"AeadCipherTcpFilter AfterReading filterContext.Memory.IsEmpty");
                }
            }
            return r;
        }

        public override PipeFilterResult BeforeWriting(PipeFilterContext ctx)
        {
            PipeFilterResult r = new PipeFilterResult(this.Client, null, false);
            if (null != _cipher)
            {
                if (!ctx.Memory.IsEmpty)
                {
                    var bufferCipher = _cipher.EncryptUdp(ctx.Memory.Slice(0, ctx.MemoryLength));
                    r = new PipeFilterResult(this.Client, bufferCipher, true);
                }
                else
                {
                    _logger?.LogError($"AeadCipherTcpFilter BeforeWriting filterContext.Memory.IsEmpty");
                }
            }
            return r;
        }
    }
}