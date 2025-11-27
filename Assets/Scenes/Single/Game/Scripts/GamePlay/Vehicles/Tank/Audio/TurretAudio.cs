using UnityEngine;

public class TurretAudio
{
    readonly TurretAiming o;
    readonly TurretYaw yaw;

    readonly AudioSource s;

    public TurretAudio(TurretAiming o, TurretYaw yaw)
    {
        this.o = o;
        this.yaw = yaw;

        s = o.gameObject.AddComponent<AudioSource>();
        s.clip = o.yawLoopSound;
        s.loop = true;
        s.playOnAwake = false;
        s.volume = 0f;
        s.spatialBlend = 1f;
        s.dopplerLevel = 0f;

        AudioManager.AssignToMaster(s);
    }

    public void UpdateAudio(bool rotating)
    {
        float normalized = yaw.GetNormalizedSpeed();

        float volume = Mathf.Lerp(o.yawMinVolume, o.yawMaxVolume, normalized);
        float pitch = Mathf.Lerp(o.yawPitchRange.x, o.yawPitchRange.y, normalized);

        if (rotating && (o.sniperView == null || !o.sniperView.IsSniperActive()))
        {
            // если снайпер OFF — звук OFF
            s.Stop();
            return;
        }

        if (rotating)
        {
            if (!s.isPlaying) s.Play();

            s.volume = Mathf.Lerp(s.volume, volume, o.audioFadeSpeed * Time.deltaTime);
            s.pitch = Mathf.Lerp(s.pitch, pitch, o.audioFadeSpeed * Time.deltaTime);
        }
        else
        {
            s.volume = Mathf.Lerp(s.volume, 0f, o.audioFadeSpeed * Time.deltaTime);
            if (s.volume < 0.01f && s.isPlaying)
                s.Stop();
        }
    }
}
