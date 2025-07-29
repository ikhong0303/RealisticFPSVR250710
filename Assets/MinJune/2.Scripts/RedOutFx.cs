using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class RedOutFx : MonoBehaviour
{
    public Renderer renderer; //시뻘건 렌더러
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    public void RedOut(float _hp)
    {
        //var mat = renderer.materials[0]; //현재 마테리얼 정보를 가져옴
        // mat.SetFloat("_ApertureSize", _hp);
        renderer.materials[0].SetFloat("_ApertureSize", _hp);
    }
}
