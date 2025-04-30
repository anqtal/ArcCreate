using System;
using System.Collections.Generic;

namespace ArcCreate.Remote.Common
{
    // Message layout is:
    // |-2 bytes-|-4 bytes-|-(Length)bytes-|
    // |-Control-|-Length--|-----Data------|
    // This supports: 256 control signal, 4GB transfer.
    public class MessagePackager
    {
        public const int ControlBytes = 2;
        public const int LengthBytes = 4;

        private readonly byte[] headerBytes = new byte[ControlBytes + LengthBytes];

        private readonly Queue<(RemoteControl control, byte[] message)> messageQueue = new();
        private int headerBytesSoFar;
        private byte[] messageBytes;
        private int messageBytesSoFar;

        public void ProcessMessage(byte[] buffer, int offset, int length)
        {
            if (length <= 0) return;

            length += offset;

            var bufferIndex = offset;
            for (var i = headerBytesSoFar; i < Math.Min(headerBytes.Length, length); i++)
            {
                headerBytes[i] = buffer[bufferIndex];
                bufferIndex += 1;
            }

            headerBytesSoFar = Math.Min(headerBytes.Length, length);

            if (messageBytes == null && headerBytesSoFar == headerBytes.Length)
            {
                var messageLength = BitConverter.ToUInt32(headerBytes, ControlBytes);
                if (messageLength == 0)
                {
                    var controlBytes = new byte[ControlBytes];
                    Array.Copy(headerBytes, controlBytes, ControlBytes);
                    var controlInt = BitConverter.ToInt16(controlBytes, 0);
                    var control = (RemoteControl)controlInt;

                    messageQueue.Enqueue((control, new byte[0]));
                    messageBytes = null;
                    headerBytesSoFar = 0;
                    messageBytesSoFar = 0;
                    ProcessMessage(buffer, bufferIndex, length - bufferIndex);
                    return;
                }

                messageBytes = new byte[messageLength];
                messageBytesSoFar = 0;
            }

            if (messageBytes != null)
                for (var i = bufferIndex; i < length; i++)
                {
                    messageBytes[messageBytesSoFar] = buffer[i];
                    messageBytesSoFar += 1;

                    if (messageBytesSoFar == messageBytes.Length)
                    {
                        var controlBytes = new byte[ControlBytes];
                        Array.Copy(headerBytes, controlBytes, ControlBytes);
                        var controlInt = BitConverter.ToInt16(controlBytes, 0);
                        var control = (RemoteControl)controlInt;

                        messageQueue.Enqueue((control, messageBytes));
                        messageBytes = null;
                        headerBytesSoFar = 0;
                        messageBytesSoFar = 0;
                        ProcessMessage(buffer, i + 1, length - i - 1);
                        return;
                    }
                }
        }

        public bool HasQueuedMessage(out RemoteControl control, out byte[] message)
        {
            if (messageQueue.Count > 0)
            {
                (control, message) = messageQueue.Dequeue();
                return true;
            }

            control = RemoteControl.Invalid;
            message = null;
            return false;
        }

        public byte[] CreateHeader(RemoteControl control, int length)
        {
            var result = new byte[ControlBytes + LengthBytes];

            var controlBytes = BitConverter.GetBytes((short)control);
            var lengthBytes = BitConverter.GetBytes((uint)length);

            Array.Copy(controlBytes, 0, result, 0, ControlBytes);
            Array.Copy(lengthBytes, 0, result, ControlBytes, LengthBytes);

            return result;
        }
    }
}