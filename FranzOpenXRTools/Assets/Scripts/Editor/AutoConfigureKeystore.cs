using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoConfigureKeystore
{
    static AutoConfigureKeystore()
    {
        // 确保是在Android平台上
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            // 设置keystore的相关信息
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = "Keystore/franz.keystore"; // 替换为你的keystore文件路径
            PlayerSettings.Android.keystorePass = "111111"; // 替换为你的keystore密码
            PlayerSettings.Android.keyaliasName = "franz"; // 替换为你的密钥别名
            PlayerSettings.Android.keyaliasPass = "111111"; // 替换为你的密钥密码
        }
    }
}
