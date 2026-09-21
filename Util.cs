using System;
using System.Linq;
using Modding;
using UnityEngine;
using UnityEngine.Audio;

namespace MusicShuffle
{
    public static class Util
    {
        public static MusicCue CreateMusicCueFromClip(AudioClip clip)
        {
            MusicCue newCue = new()
            {
                name = clip.name
            };

            MusicCue.MusicChannelInfo[] channels = new MusicCue.MusicChannelInfo[6];
            MusicCue.MusicChannelInfo channel = new();
            ReflectionHelper.SetField(channel, "clip", clip);
            ReflectionHelper.SetField(channel, "sync", MusicChannelSync.Implicit);
            channels[0] = channel;
            MusicCue.MusicChannelInfo emptyChannel = new();
            channels[1] = channels[2] = channels[3] = channels[4] = channels[5] = emptyChannel;
            ReflectionHelper.SetField(newCue, "channelInfos", channels);

            return newCue;
        }

        public static MusicChannels[] GetChannelsForMusicCue(MusicCue cue)
        {
            MusicChannels[] activeChannels = [];
            foreach (MusicChannels channel in Enum.GetValues(typeof(MusicChannels)))
            {
                MusicCue.MusicChannelInfo info = cue.GetChannelInfo(channel);
                if (info == null) continue;
                AudioClip clip = info.Clip;
                if (clip == null) continue;
                activeChannels = [.. activeChannels.Append(channel)];
            }
            return activeChannels;
        }
    }
}

