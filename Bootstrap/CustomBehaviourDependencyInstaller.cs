#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Vtcong.Bootstrap
{
    /// <summary>
    /// Tự cài com.vtcong.singleton qua git URL nếu project chưa có.
    ///
    /// Unity Package Manager không phân giải được git URL khai báo trong trường
    /// "dependencies" của package.json — chỉ manifest.json của project mới nhận git URL.
    /// Nên phải tự gọi Client.Add lúc editor khởi động.
    ///
    /// Assembly này cố ý KHÔNG tham chiếu tới assembly nào của package. Nếu nó tham chiếu
    /// runtime assembly thì khi thiếu Singleton, runtime lỗi biên dịch kéo theo assembly
    /// này cũng lỗi, và script sẽ không bao giờ chạy để sửa chính vấn đề đó.
    /// </summary>
    [InitializeOnLoad]
    internal static class CustomBehaviourDependencyInstaller
    {
        private const string PackageId = "com.vtcong.singleton";
        private const string GitUrl = "https://github.com/vtcong2299/VTC_Singleton.git";
        private const string ProbeType = "Vtcong.Core.Singleton`1";
        private const string Label = "VTC Custom Behaviour";
        private const string SessionKey = "vtcong.singleton.install.attempted";

        private static ListRequest _list;
        private static AddRequest _add;

        static CustomBehaviourDependencyInstaller()
        {
            EditorApplication.delayCall += Begin;
        }

        private static void Begin()
        {
            // Đã thử trong phiên editor này rồi thì thôi, tránh lặp vô hạn khi cài lỗi.
            if (SessionState.GetBool(SessionKey, false)) return;

            // Kiểm tra theo kiểu chứ không theo tên package: bắt được cả trường hợp
            // VTC_Singleton nằm thẳng trong Assets/ thay vì cài qua Package Manager.
            if (HasSingletonType()) return;

            SessionState.SetBool(SessionKey, true);
            _list = Client.List(true, false);
            EditorApplication.update += PollList;
        }

        private static bool HasSingletonType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.GetType(ProbeType, false) != null) return true;
                }
                catch
                {
                    // Assembly động hoặc không load được: bỏ qua.
                }
            }

            return false;
        }

        private static void PollList()
        {
            if (_list == null || !_list.IsCompleted) return;
            EditorApplication.update -= PollList;

            if (_list.Status == StatusCode.Success)
                foreach (var package in _list.Result)
                    if (package.name == PackageId)
                    {
                        // Đã có trong manifest, chỉ là chưa biên dịch xong. Không cài lại.
                        _list = null;
                        return;
                    }

            _list = null;
            Debug.Log($"[{Label}] Thiếu {PackageId}, đang cài từ {GitUrl} ...");
            _add = Client.Add(GitUrl);
            EditorApplication.update += PollAdd;
        }

        private static void PollAdd()
        {
            if (_add == null || !_add.IsCompleted) return;
            EditorApplication.update -= PollAdd;

            if (_add.Status == StatusCode.Success)
                Debug.Log($"[{Label}] Đã cài {_add.Result.packageId}.");
            else
                Debug.LogError(
                    $"[{Label}] Không cài được {PackageId}: {_add.Error?.message}\n" +
                    $"Cài thủ công: Window ▸ Package Manager ▸ + ▸ Add package from git URL ▸ {GitUrl}");

            _add = null;
        }
    }
}
#endif
