using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public static class WaitForCompilation
{
    [MenuItem("TD/Wait For Compilation Test")]
    public static void Test()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.Log("[WaitForCompilation] ⏳ Compilation in progress, waiting...");
        }
        else
        {
            Debug.Log("[WaitForCompilation] ✓ No compilation running");
        }
    }

    public static bool IsCompiling()
    {
        return EditorApplication.isCompiling;
    }

    public static void LogStatus()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.Log("[WaitForCompilation] ⏳ COMPILING");
        }
        else
        {
            Debug.Log("[WaitForCompilation] ✓ READY");
        }
    }
}
