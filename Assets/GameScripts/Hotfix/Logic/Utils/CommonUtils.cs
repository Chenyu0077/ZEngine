//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------


using UnityEngine;

public class CommonUtils
{
    ///<summary>
    ///根据面向和移动方向得到一个资源名预订了规则的后缀名
    ///<param name="faceDegree">面向角度</param>
    ///<param name="moveDegree">移动角度</param>
    ///<return>约定好的关键字，比如"Forward","Back","Left","Right"，对应到角色动画的key</return>
    ///</summary>
    public static string GetTailStringByDegree(float faceDegree, float moveDegree)
    {
        float fd = faceDegree;
        float md = moveDegree;
        while (fd < 180) fd += 360;
        while (md < 180) md += 360;
        fd = fd % 360;
        md = md % 360;
        float dd = md - fd;
        if (dd > 180)
        {
            dd -= 360;
        }
        else if (dd < -180)
        {
            dd += 360;
        }
        //Debug.Log("degree:"+fd + " / " + md + " / " + dd);
        if (dd >= -45 && dd <= 45)
        {
            return "Forward";
        }
        else
        if (dd < -45 && dd >= -135)
        {
            return "Left";
        }
        else
        if (dd > 45 && dd <= 135)
        {
            return "Right";
        }
        else
        {
            return "Back";
        }
    }

    /// <summary>
    /// 克隆音频源文件
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static AudioClip CloneAudioClip(AudioClip source)
    {
        float[] data = new float[source.samples * source.channels];
        source.GetData(data, 0);

        AudioClip newClip = AudioClip.Create(
            source.name + "_clone",
            source.samples,
            source.channels,
            source.frequency,
            false
        );

        newClip.SetData(data, 0);

        return newClip;
    }

    /// <summary>
    /// 克隆贴图
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Texture2D CloneTexture2D(Texture2D source)
    {
        Texture2D newTex = new Texture2D(
            source.width,
            source.height,
            source.format,
            source.mipmapCount > 1
        );

        newTex.LoadRawTextureData(source.GetRawTextureData());
        newTex.Apply();

        return newTex;
    }
}
