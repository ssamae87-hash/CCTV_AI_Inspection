# CCTV_AI_Inspection

WPF(MVVM) ����� **ONNX �߷� ���** �����Դϴ�.  
�̹��� ������ ���� �� ONNX �𵨷� �߷��ϰ� �� ȭ�鿡 **�ٿ�� �ڽ�**�� �������̷� ǥ���մϴ�.  
**MVVM�� ó�� ���ϴ� ��**�� ������ �� �ֵ��� ����/Ŭ���� ���Ұ� ������ �帧�� �ڼ��� �����մϴ�.

---

## ����
- [���� ����](#����-����)
- [������Ʈ ����](#������Ʈ-����)
- [�� ���� & Ŭ���� ����](#��-����--Ŭ����-����)
  - [App](#app)
  - [Views](#views)
  - [ViewModels](#viewmodels)
  - [Models](#models)
  - [Services](#services)
  - [Utils](#utils)
  - [Assets](#assets)
- [MVVM ���� ���� ����](#mvvm-����-����-����)
  - [Model / View / ViewModel�� ������?](#model--view--viewmodel��-������)
  - [���ε��̶�?](#���ε��̶�)
  - [INotifyPropertyChanged](#inotifypropertychanged)
  - [Command(���)�� ��ư ����](#command��ɰ�-��ư-����)
  - [ItemsControl + Canvas�� �ڽ� ���������ϴ� ����](#itemscontrol--canvas��-�ڽ�-���������ϴ�-����)
- [������ �帧 (������)](#������-�帧-������)
- [�߷� ���������� ���](#�߷�-����������-���)
- [�� ȣȯ�� & ���� ����](#��-ȣȯ��--����-����)
- [Ȯ�� ���̵��](#Ȯ��-���̵��)
- [���� ������ ���� & �ذ�å](#����-������-����--�ذ�å)
- [���� ��](#����-��)
- [���̼���](#���̼���)

---

## ���� ����
1. **�ʼ� NuGet ��Ű��**
   - `Microsoft.ML.OnnxRuntime` (CPU �߷�)
   - `OpenCvSharp4.Windows`
   - `OpenCvSharp4.Extensions`

2. **���� ����**
   - �� ���� �� **�� ����(.onnx)** �� **�̹��� ����** �� **�߷� ����**
   - �����̴��� `Conf`(�ŷڵ� �Ӱ谪), `NMS`(�ڽ� �ߺ� ���� �Ӱ谪) ����

> �⺻ Ÿ�� �����ӿ�ũ: **.NET Framework 4.7.2** (�ʿ� �� 4.6.2+�� ���� ����)

---

## ������Ʈ ����
```
CCTV_AI_Inspection.sln
CCTV_AI_Inspection/
 ���� App.xaml
 ���� App.xaml.cs
 ���� Views/
 ��   ���� MainWindow.xaml
 ��       MainWindow.xaml.cs
 ���� ViewModels/
 ��   ���� MainViewModel.cs
 ���� Models/
 ��   ���� DetectionResult.cs
 ���� Services/
 ��   ���� OnnxYoloDetector.cs
 ���� Utils/
 ��   ���� RelayCommand.cs
 ��   ���� ImageUtils.cs
 ���� Assets/           # (����) ���� �̹��� ��
```

---

## �� ���� & Ŭ���� ����

### App
- **App.xaml / App.xaml.cs**
  - WPF ���ø����̼��� �������Դϴ�.
  - `StartupUri="Views/MainWindow.xaml"`�� ù ȭ���� �����մϴ�.
  - ���� ���ҽ�/��Ÿ���� ������ ���� �ֽ��ϴ�.

### Views
- **MainWindow.xaml / MainWindow.xaml.cs**
  - ȭ��(UI)�� �����ϴ� XAML�Դϴ�.
  - *�߿�*: �ڵ�����ε�(.cs)������ **������ ���� �ʰ�** `DataContext = new MainViewModel()`�� �����մϴ�.
  - ���� �г�(���� ����/�� ����/�����̴�/��ư), ���� �г�(�̹����� �ڽ� ��������)�� �����˴ϴ�.
  - **View�� ���� ȭ��**���� �����մϴ�. ������ ViewModel/Service�� �и��մϴ�.

### ViewModels
- **MainViewModel.cs**
  - ȭ��� ���ε��Ǵ� **�Ӽ�/���(Command)** �� �����մϴ�.
    - `ImagePath`, `ModelPath`, `DisplayImage`, `Status`, `ConfThreshold`, `NmsThreshold`
    - `OpenImageCommand`, `OpenModelCommand`, `InferenceCommand`
  - `INotifyPropertyChanged` �������� ���ε��� ���� �ٲ�� ȭ�鿡 �ڵ� �ݿ��˴ϴ�.
  - ���� �߷��� `Services.OnnxYoloDetector`�� �����ϰ�, ����� `ObservableCollection<DetectionResult>` (`Results`)�� ��� View�� �����մϴ�.

### Models
- **DetectionResult.cs**
  - �� ���� ���� ����� ǥ���ϴ� **DTO**(Data Transfer Object).
  - View���� `ItemsControl + Canvas`�� ��ġ�ϱ� ���� `X, Y, Width, Height` (ĵ���� ��ǥ��)�� `Label`, `Score`�� �����մϴ�.
  - ���� ������ �����̳��̹Ƿ� ������ �����ϴ�.

### Services
- **OnnxYoloDetector.cs**
  - **ONNX Runtime**���� YOLO �迭 ���� **CPU �߷�**�ϴ� ����.
  - �ٽ� å��
    1. �� �ε�/���� (`Load`, `Dispose`)
    2. ��ó�� (Letterbox 640��640, BGR��RGB, [0,1] ����ȭ)
    3. �߷� ���� (`InferenceSession.Run`)
    4. ��ó�� (��� �Ľ�, �ŷڵ� ���͸�, **NMS**, ���� ��ǥ ������)
  - Ư¡
    - **unsafe ��� ����**. `Marshal.Copy` ���� �̿��� �����ϰ� �ټ��� ����.
    - ��� �ټ��� **�⺻ ����**: `[1, N, 85]` (xywh + obj + 80 class).  
      �� �𵨿� ���� `[1, 85, N]` �� �ٸ� �� �����Ƿ� **�Ľ̺θ� ��ü**�ϸ� �˴ϴ�.
    - NMS/IoU ���� ����.

### Utils
- **RelayCommand.cs**
  - MVVM���� ��ư�� ������ �����ϴ� **Ŀ���� ICommand**.
  - ��ư�� ������ ������ `Action<object?>`�� ����˴ϴ�.
  - `CanExecute`�� ���� ��ư Ȱ��/��Ȱ�� ���� ����.
- **ImageUtils.cs**
  - �̹��� ������ `BitmapSource`�� �����ϰ� �ε��մϴ�.
  - `BitmapCacheOption.OnLoad`�� ���� �ڵ� ����� �����մϴ�(��� ����).

### Assets
- ���� �̹���/�߰� ���ҽ��� �����ϴ� ����(����).

---

## MVVM ���� ���� ����

### Model / View / ViewModel�� ������?
- **Model**: ���� ������(���, ��ƼƼ). ���� ����.
- **View**: ȭ��(XAML). **������/���̾ƿ���** ���.
- **ViewModel**: ȭ��� ������ **�߰� �Ŵ���**.  
  - ȭ�鿡 ������ ��(�Ӽ�)��, ��ư Ŭ�� �� ������ **���(Command)** �� �����մϴ�.
  - View�� ViewModel�� �Ӽ�/��ɿ� **���ε�**�� �ϸ� �˴ϴ�.

> ����:  
> - *View* = TV ȭ��  
> - *ViewModel* = ������(ȭ�鿡 � �� �������� ����)  
> - *Model* = ���� ������(����/�ڸ� ��)

### ���ε��̶�?
- XAML���� `{Binding Status}` ó�� ����,
  - View�� **Status**�� �̸��� �Ӽ��� ViewModel���� ã�� **�ڵ����� �� �ݿ�**.
  - ViewModel�� `Status = "�Ϸ�"`�� �ٲٰ� `PropertyChanged`�� �ҷ��ָ�, ȭ�� �ؽ�Ʈ�� �ڵ� ����.

### INotifyPropertyChanged
- ViewModel�� �� �������̽��� �����ؾ� **�� ���� �� ȭ�� �ڵ� ������Ʈ**�� �˴ϴ�.
- ��:  
  ```csharp
  private string _status;
  public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
  ```
  `OnPropertyChanged()`�� ȣ��Ǹ� XAML ���ε��� �� ��ȭ�� �����մϴ�.

### Command(���)�� ��ư ����
- XAML:
  ```xml
  <Button Content="�߷� ����" Command="{Binding InferenceCommand}"/>
  ```
- ViewModel:
  ```csharp
  public RelayCommand InferenceCommand { get; }
  InferenceCommand = new RelayCommand(_ => Inference(), _ => CanRun());
  ```
  - ��ư Ŭ�� �� `Inference()` ����
  - `_ => CanRun()`�� false�� ��ư ��Ȱ��ȭ

### ItemsControl + Canvas�� �ڽ� ���������ϴ� ����
- �̹��� ���� **���� �� �ڽ�**�� ���� �׷��� �մϴ�.
- `ItemsControl`�� **����Ʈ ���ε�**�� ���ϰ�, `ItemsPanelTemplate`�� `Canvas`�� �ٲٸ�  
  �� �׸��� `Canvas.Left/Top`�� ���ε��ؼ� **��Ȯ�� ��ǥ�� ��ġ**�� �� �ֽ��ϴ�.

---

## ������ �帧 (������)
1. ����ڰ� **�� ����** �� `MainViewModel.OpenModel()` �� `OnnxYoloDetector.Load()`  
2. ����ڰ� **�̹��� ����** �� `MainViewModel.OpenImage()` �� `DisplayImage`�� ǥ��  
3. ����ڰ� **�߷� ����** �� `MainViewModel.Inference()`  
   - `OnnxYoloDetector.Run()` ȣ��
   - ��ó��(��������е�������ȭ) �� �߷� �� ��ó��(NMS, ��ǥ ����)
   - `List<DetectionResult>` ��ȯ �� `Results`�� ���  
4. View�� `Results`�� `ItemsControl`�� �׷��� �ڽ� �������� ǥ��

---

## �߷� ���������� ���
1. **Letterbox(���� ���� �������� + �е�)** : 640��640, pad ���� (114,114,114)  
2. **BGR �� RGB**, **float32**, **/255.0** ����ȭ  
3. **HWC �� CHW**�� ��迭�� �ټ� ���� (`[1,3,640,640]`)  
4. **Run(inputs)** ���� �� 1st output�� float �迭�� ����  
5. **��� �Ľ�**
   - ����: `[1, N, 85]` (x, y, w, h, obj, cls0..cls79)
   - `score = obj * max(classScores)`  
   - `score < conf`�� ����  
   - `xywh �� xyxy` ��ȯ  
6. **���� ��ǥ ����** (�е�/������ ������)  
7. **NMS**�� �ߺ� �ڽ� ����  
8. **DetectionResult**�� ��ȯ �� View�� ���ε�

---

## �� ȣȯ�� & ���� ����
- YOLO ����ġ/Export ��Ŀ� ���� **��� �ټ� shape**�� �ٸ� �� �ֽ��ϴ�.
  - ��: `[1, 85, N]` ���� �� **transpose** �ʿ�
  - Ŭ���� ����(80)�� �ٸ��� `85` ���(= 5 + num_classes)�� ����
- ���� ����Ʈ:
  - `OnnxYoloDetector.Run()`�� **��� �Ľ̺�**  
  - �ʿ� �� `input` �̸�, `imgsz`, ��ó�� ����� �𵨿� �°� ����

---

## Ȯ�� ���̵��
- **Ŭ���� �̸� ����**: `classes.txt`�� �ε��� `cls0 �� "person"`ó�� ǥ��
- **���׸����̼� �߰�**: `mode = det/seg` �ɼ� �� ����ũ ��������(WriteableBitmap�� ä��)
- **���� �ҽ�**: ����/RTSP/USB ī�޶�. `DispatcherTimer` �Ǵ� `Task`�� ������ ����
- **GPU �߷� ���**: ȯ���� �Ǹ� `OnnxRuntime.Gpu` ��Ű���� ��ȯ
- **���� ����ȭ**: ���� ����, ���� �迭, pinned �޸�, ��ġ �߷�

---

## ���� ������ ���� & �ذ�å

### 1) `CS0128` (���� ������ �̹� ���ǵ�)
- ����: ���� �������� **���� �̸� ����**�� �� �� ����.
- ��: `using var results = _session.Run(...);` �Ʒ����� �ٽ� `var results = new List<DetectionResult>();`
- �ذ�: �̸��� �����ϼ���.  
  - `_session` ��� �� `outputs`  
  - ��ó�� ����Ʈ �� `detections`

### 2) `OnnxRuntimeException: invalid dimensions (input 640 vs got 384)`
- ����: �� �Է� ũ��� ��ó�� ũ�� ����ġ.
- �ذ�: ��ó���� `inputSize`(�⺻ 640)�� **���� �䱸�ϴ� �Է� ũ�⸦ ���߱�**.

### 3) �ڽ��� ������ ��ġ�� �׸�
- ����: **Letterbox ������ ������** �Ǵ� **��ǥ Ŭ���� ����**.
- �ذ�: �е�/������ ������ ������ Ȯ���ϰ�, `0~(W-1/H-1)` ������ Ŭ����.

### 4) OpenCV DLL �ε� ����
- ����: `OpenCvSharp4.Windows` �̼�ġ, �Ǵ� VC++ ������� ����.
- �ذ�: NuGet �缳ġ, �ʿ� �� VC++ ������� ��ġ.

---

## ���� ��
- **CPU ����**�� ���:
  - `GraphOptimizationLevel = ORT_ENABLE_ALL`
  - ���ʿ��� �Ҵ� ���̱�(���� ����)
- **�̹��� ũ��**�� Ŀ�� �����̸�:
  - `inputSize`�� 640 ���� ���� / �Ǵ� �𵨿� ���� ����
- **NMS �Ӱ谪**�� ������ �ڽ� �� ���̱�(0.4~0.6 ����)

---

## ���̼���
- �������丮�� �ֻ��� `LICENSE` ������ �����ϼ���. (���ٸ� �� ���۱� ��å�� ���� �߰��ϼ���)

---

### �η�: ���̹� ������(����)
- ONNX ��� �÷���: `outputs`
- ��ó���� ���(���ε���): `detections`  
- �浹 ����/�������� ���� ��� �������� ��ó�� **�ϰ��ǰ�** ����ϼ���.
