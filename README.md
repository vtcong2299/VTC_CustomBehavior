# VTC Custom Behaviour

Gom `Update` / `FixedUpdate` / `LateUpdate` của hàng nghìn MonoBehaviour về **một** vòng
lặp tập trung, bỏ chi phí gọi message của Unity trên từng object.

## Cài đặt

Unity ▸ **Window ▸ Package Manager ▸ + ▸ Add package from git URL**:

```
https://github.com/vtcong2299/VTC_CustomBehavior.git
```

> **Phụ thuộc [VTC Singleton](https://github.com/vtcong2299/VTC_Singleton)** — **package tự
> cài giúp bạn**, không cần làm gì thêm. Xem mục cuối nếu muốn hiểu cách nó hoạt động.

## Cập nhật

Mỗi lần mở Unity Editor, package kiểm tra tất cả các package `com.vtcong.*` đang cài qua git URL:

- Bản thấp hơn 1.1.0 được tự nâng lên `#v1.1.0`, không cần thao tác.
- Có bản mới hơn trên GitHub thì hiện hộp thoại: **Cập nhật**, **Để sau** (hỏi lại lần mở sau) hoặc **Bỏ qua bản này**.

Không chạy trong batchmode (CI). Mất mạng thì bỏ qua, không báo lỗi.

## Dùng

Kế thừa `CustomBehaviour` và implement vòng lặp bạn cần:

```csharp
public class Rotator : CustomBehaviour, IUpdateBehaviour
{
    [SerializeField] private Vector3 degreesPerSecond = new(0f, 90f, 0f);

    public void OnBehaviourUpdate(float dt)
    {
        transform.Rotate(degreesPerSecond * dt, Space.Self);
    }
}
```

Đăng ký và huỷ đăng ký là tự động. `OnEnable` / `OnDisable` / `Start` bị seal có chủ ý —
override `OnBehaviourEnable` / `OnBehaviourDisable` / `OnBehaviourStart` thay thế, vì
khai báo đè các message gốc sẽ làm object không bao giờ đăng ký được.

| Interface | Vòng lặp |
|---|---|
| `IUpdateBehaviour` | `OnBehaviourUpdate(float dt)` |
| `IFixedUpdateBehaviour` | `OnBehaviourFixedUpdate(float dt)` |
| `ILateUpdateBehaviour` | `OnBehaviourLateUpdate(float dt)` |

## Examples

Sample **Examples** có các behaviour mẫu và `BehaviourRegressionTests` — import qua
Package Manager (xem mục cuối), chạy scene rồi xem kết quả hiện trên màn hình.

## Dependency được cài tự động thế nào

Unity Package Manager **không** phân giải được git URL khai báo trong trường
`dependencies` của `package.json` — chỉ `manifest.json` của project mới nhận git URL.

Nên package này kèm một assembly Editor nhỏ (`*.bootstrap`) chạy lúc editor khởi động:
nó kiểm tra kiểu `Vtcong.Core.Singleton<T>` có tồn tại không, nếu không thì gọi
`Client.Add(gitUrl)` để cài VTC Singleton. Assembly đó cố ý không tham chiếu tới
assembly nào của package, để khi thiếu Singleton nó vẫn biên dịch được và chạy để sửa.

Kiểm tra theo kiểu chứ không theo tên package, nên nếu bạn đã có VTC_Singleton nằm
thẳng trong `Assets/` thì nó sẽ không cài trùng.

Muốn khai báo tay thì thêm vào `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.vtcong.singleton": "https://github.com/vtcong2299/VTC_Singleton.git",
    "com.vtcong.custombehaviour": "https://github.com/vtcong2299/VTC_CustomBehavior.git"
  }
}
```

## Mở scene sample

Sample **không** được import sẵn. Vào **Window ▸ Package Manager ▸ VTC Custom Behaviour ▸ Samples ▸ Import**.

Unity copy sample vào `Assets/Samples/VTC Custom Behaviour/<version>/Examples/`, từ đó mở scene bình thường.

> **Vì sao phải import?** Package cài qua git URL nằm trong `Library/PackageCache` và ở
> chế độ read-only. Unity **cấm mở scene nằm trong package read-only** — báo
> *"It is not allowed to open a scene in a read-only package"*. Cơ chế Samples của UPM
> sinh ra chính để giải quyết việc này: nó copy sample sang `Assets/` nơi file ghi được.
