# CCTV_AI_Inspection

WPF(MVVM) 기반의 **ONNX 추론 뷰어** 예제입니다.  
이미지 파일을 열고 → ONNX 모델로 추론하고 → 화면에 **바운딩 박스**를 오버레이로 표시합니다.  
**MVVM을 처음 접하는 분**도 이해할 수 있도록 폴더/클래스 역할과 데이터 흐름을 자세히 설명합니다.

---

## 목차
- [빠른 실행](#빠른-실행)
- [프로젝트 구조](#프로젝트-구조)
- [각 폴더 & 클래스 역할](#각-폴더--클래스-역할)
  - [App](#app)
  - [Views](#views)
  - [ViewModels](#viewmodels)
  - [Models](#models)
  - [Services](#services)
  - [Utils](#utils)
  - [Assets](#assets)
- [MVVM 아주 쉽게 설명](#mvvm-아주-쉽게-설명)
  - [Model / View / ViewModel이 뭔가요?](#model--view--viewmodel이-뭔가요)
  - [바인딩이란?](#바인딩이란)
  - [INotifyPropertyChanged](#inotifypropertychanged)
  - [Command(명령)과 버튼 연결](#command명령과-버튼-연결)
  - [ItemsControl + Canvas로 박스 오버레이하는 이유](#itemscontrol--canvas로-박스-오버레이하는-이유)
- [데이터 흐름 (시퀀스)](#데이터-흐름-시퀀스)
- [추론 파이프라인 요약](#추론-파이프라인-요약)
- [모델 호환성 & 변경 지점](#모델-호환성--변경-지점)
- [확장 아이디어](#확장-아이디어)
- [자주 만나는 오류 & 해결책](#자주-만나는-오류--해결책)
- [성능 팁](#성능-팁)
- [라이선스](#라이선스)

---

## 빠른 실행
1. **필수 NuGet 패키지**
   - `Microsoft.ML.OnnxRuntime` (CPU 추론)
   - `OpenCvSharp4.Windows`
   - `OpenCvSharp4.Extensions`

2. **실행 순서**
   - 앱 실행 → **모델 열기(.onnx)** → **이미지 열기** → **추론 실행**
   - 슬라이더로 `Conf`(신뢰도 임계값), `NMS`(박스 중복 제거 임계값) 조절

> 기본 타깃 프레임워크: **.NET Framework 4.7.2** (필요 시 4.6.2+로 변경 가능)

---

## 프로젝트 구조
```
CCTV_AI_Inspection.sln
CCTV_AI_Inspection/
 ├─ App.xaml
 ├─ App.xaml.cs
 ├─ Views/
 │   └─ MainWindow.xaml
 │       MainWindow.xaml.cs
 ├─ ViewModels/
 │   └─ MainViewModel.cs
 ├─ Models/
 │   └─ DetectionResult.cs
 ├─ Services/
 │   └─ OnnxYoloDetector.cs
 ├─ Utils/
 │   ├─ RelayCommand.cs
 │   └─ ImageUtils.cs
 └─ Assets/           # (선택) 데모 이미지 등
```

---

## 각 폴더 & 클래스 역할

### App
- **App.xaml / App.xaml.cs**
  - WPF 애플리케이션의 시작점입니다.
  - `StartupUri="Views/MainWindow.xaml"`로 첫 화면을 지정합니다.
  - 전역 리소스/스타일을 정의할 수도 있습니다.

### Views
- **MainWindow.xaml / MainWindow.xaml.cs**
  - 화면(UI)을 정의하는 XAML입니다.
  - *중요*: 코드비하인드(.cs)에서는 **로직을 넣지 않고** `DataContext = new MainViewModel()`만 설정합니다.
  - 좌측 패널(파일 열기/모델 열기/슬라이더/버튼), 우측 패널(이미지와 박스 오버레이)로 구성됩니다.
  - **View는 오직 화면**에만 집중합니다. 로직은 ViewModel/Service로 분리합니다.

### ViewModels
- **MainViewModel.cs**
  - 화면과 바인딩되는 **속성/명령(Command)** 을 제공합니다.
    - `ImagePath`, `ModelPath`, `DisplayImage`, `Status`, `ConfThreshold`, `NmsThreshold`
    - `OpenImageCommand`, `OpenModelCommand`, `InferenceCommand`
  - `INotifyPropertyChanged` 구현으로 바인딩된 값이 바뀌면 화면에 자동 반영됩니다.
  - 실제 추론은 `Services.OnnxYoloDetector`에 위임하고, 결과는 `ObservableCollection<DetectionResult>` (`Results`)에 담아 View로 전달합니다.

### Models
- **DetectionResult.cs**
  - 한 개의 검출 결과를 표현하는 **DTO**(Data Transfer Object).
  - View에서 `ItemsControl + Canvas`로 배치하기 쉽게 `X, Y, Width, Height` (캔버스 좌표계)와 `Label`, `Score`를 제공합니다.
  - 순수 데이터 컨테이너이므로 로직이 없습니다.

### Services
- **OnnxYoloDetector.cs**
  - **ONNX Runtime**으로 YOLO 계열 모델을 **CPU 추론**하는 서비스.
  - 핵심 책임
    1. 모델 로드/해제 (`Load`, `Dispose`)
    2. 전처리 (Letterbox 640×640, BGR→RGB, [0,1] 정규화)
    3. 추론 실행 (`InferenceSession.Run`)
    4. 후처리 (출력 파싱, 신뢰도 필터링, **NMS**, 원본 좌표 역보정)
  - 특징
    - **unsafe 사용 없음**. `Marshal.Copy` 등을 이용해 안전하게 텐서를 구성.
    - 출력 텐서는 **기본 가정**: `[1, N, 85]` (xywh + obj + 80 class).  
      → 모델에 따라 `[1, 85, N]` 등 다를 수 있으므로 **파싱부만 교체**하면 됩니다.
    - NMS/IoU 구현 포함.

### Utils
- **RelayCommand.cs**
  - MVVM에서 버튼과 로직을 연결하는 **커스텀 ICommand**.
  - 버튼이 눌리면 지정한 `Action<object?>`이 실행됩니다.
  - `CanExecute`를 통해 버튼 활성/비활성 제어 가능.
- **ImageUtils.cs**
  - 이미지 파일을 `BitmapSource`로 안전하게 로드합니다.
  - `BitmapCacheOption.OnLoad`로 파일 핸들 잠금을 방지합니다(즉시 해제).

### Assets
- 데모 이미지/추가 리소스를 보관하는 폴더(선택).

---

## MVVM 아주 쉽게 설명

### Model / View / ViewModel이 뭔가요?
- **Model**: 순수 데이터(결과, 엔티티). 로직 없음.
- **View**: 화면(XAML). **디자인/레이아웃만** 담당.
- **ViewModel**: 화면과 로직의 **중간 매니저**.  
  - 화면에 보여줄 값(속성)과, 버튼 클릭 시 수행할 **명령(Command)** 을 제공합니다.
  - View는 ViewModel의 속성/명령에 **바인딩**만 하면 됩니다.

> 비유:  
> - *View* = TV 화면  
> - *ViewModel* = 리모컨(화면에 어떤 걸 보여줄지 지시)  
> - *Model* = 실제 데이터(영상/자막 등)

### 바인딩이란?
- XAML에서 `{Binding Status}` 처럼 쓰면,
  - View는 **Status**란 이름의 속성을 ViewModel에서 찾아 **자동으로 값 반영**.
  - ViewModel이 `Status = "완료"`로 바꾸고 `PropertyChanged`를 불러주면, 화면 텍스트도 자동 갱신.

### INotifyPropertyChanged
- ViewModel이 이 인터페이스를 구현해야 **값 변경 → 화면 자동 업데이트**가 됩니다.
- 예:  
  ```csharp
  private string _status;
  public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
  ```
  `OnPropertyChanged()`가 호출되면 XAML 바인딩이 그 변화를 감지합니다.

### Command(명령)과 버튼 연결
- XAML:
  ```xml
  <Button Content="추론 실행" Command="{Binding InferenceCommand}"/>
  ```
- ViewModel:
  ```csharp
  public RelayCommand InferenceCommand { get; }
  InferenceCommand = new RelayCommand(_ => Inference(), _ => CanRun());
  ```
  - 버튼 클릭 → `Inference()` 실행
  - `_ => CanRun()`이 false면 버튼 비활성화

### ItemsControl + Canvas로 박스 오버레이하는 이유
- 이미지 위에 **여러 개 박스**를 겹쳐 그려야 합니다.
- `ItemsControl`은 **리스트 바인딩**에 강하고, `ItemsPanelTemplate`을 `Canvas`로 바꾸면  
  각 항목의 `Canvas.Left/Top`에 바인딩해서 **정확한 좌표에 배치**할 수 있습니다.

---

## 데이터 흐름 (시퀀스)
1. 사용자가 **모델 열기** → `MainViewModel.OpenModel()` → `OnnxYoloDetector.Load()`  
2. 사용자가 **이미지 열기** → `MainViewModel.OpenImage()` → `DisplayImage`에 표시  
3. 사용자가 **추론 실행** → `MainViewModel.Inference()`  
   - `OnnxYoloDetector.Run()` 호출
   - 전처리(리사이즈·패딩·정규화) → 추론 → 후처리(NMS, 좌표 복원)
   - `List<DetectionResult>` 반환 → `Results`에 담김  
4. View는 `Results`를 `ItemsControl`로 그려서 박스 오버레이 표시

---

## 추론 파이프라인 요약
1. **Letterbox(비율 유지 리사이즈 + 패딩)** : 640×640, pad 색상 (114,114,114)  
2. **BGR → RGB**, **float32**, **/255.0** 정규화  
3. **HWC → CHW**로 재배열해 텐서 생성 (`[1,3,640,640]`)  
4. **Run(inputs)** 실행 → 1st output을 float 배열로 수령  
5. **출력 파싱**
   - 가정: `[1, N, 85]` (x, y, w, h, obj, cls0..cls79)
   - `score = obj * max(classScores)`  
   - `score < conf`는 버림  
   - `xywh → xyxy` 변환  
6. **원본 좌표 복원** (패딩/스케일 역보정)  
7. **NMS**로 중복 박스 제거  
8. **DetectionResult**로 변환 → View에 바인딩

---

## 모델 호환성 & 변경 지점
- YOLO 가중치/Export 방식에 따라 **출력 텐서 shape**이 다를 수 있습니다.
  - 예: `[1, 85, N]` 형태 → **transpose** 필요
  - 클래스 개수(80)가 다르면 `85` 상수(= 5 + num_classes)도 변경
- 수정 포인트:
  - `OnnxYoloDetector.Run()`의 **출력 파싱부**  
  - 필요 시 `input` 이름, `imgsz`, 전처리 방식을 모델에 맞게 조정

---

## 확장 아이디어
- **클래스 이름 매핑**: `classes.txt`를 로드해 `cls0 → "person"`처럼 표시
- **세그멘테이션 추가**: `mode = det/seg` 옵션 및 마스크 오버레이(WriteableBitmap로 채색)
- **영상 소스**: 파일/RTSP/USB 카메라. `DispatcherTimer` 또는 `Task`로 프레임 폴링
- **GPU 추론 토글**: 환경이 되면 `OnnxRuntime.Gpu` 패키지로 전환
- **성능 최적화**: 버퍼 재사용, 고정 배열, pinned 메모리, 배치 추론

---

## 자주 만나는 오류 & 해결책

### 1) `CS0128` (로컬 변수가 이미 정의됨)
- 원인: 동일 범위에서 **같은 이름 변수**를 두 번 선언.
- 예: `using var results = _session.Run(...);` 아래에서 다시 `var results = new List<DetectionResult>();`
- 해결: 이름을 구분하세요.  
  - `_session` 결과 → `outputs`  
  - 후처리 리스트 → `detections`

### 2) `OnnxRuntimeException: invalid dimensions (input 640 vs got 384)`
- 원인: 모델 입력 크기와 전처리 크기 불일치.
- 해결: 전처리의 `inputSize`(기본 640)와 **모델이 요구하는 입력 크기를 맞추기**.

### 3) 박스가 엉뚱한 위치에 그림
- 원인: **Letterbox 역보정 미적용** 또는 **좌표 클리핑 누락**.
- 해결: 패딩/스케일 역보정 로직을 확인하고, `0~(W-1/H-1)` 범위로 클리핑.

### 4) OpenCV DLL 로드 오류
- 원인: `OpenCvSharp4.Windows` 미설치, 또는 VC++ 재배포팩 누락.
- 해결: NuGet 재설치, 필요 시 VC++ 재배포팩 설치.

---

## 성능 팁
- **CPU 전용**인 경우:
  - `GraphOptimizationLevel = ORT_ENABLE_ALL`
  - 불필요한 할당 줄이기(버퍼 재사용)
- **이미지 크기**가 커서 병목이면:
  - `inputSize`를 640 고정 유지 / 또는 모델에 맞춰 조정
- **NMS 임계값**을 조정해 박스 수 줄이기(0.4~0.6 권장)

---

## 라이선스
- 리포지토리의 최상위 `LICENSE` 파일을 참고하세요. (없다면 원 저작권 정책에 맞춰 추가하세요)

---

### 부록: 네이밍 컨벤션(권장)
- ONNX 결과 컬렉션: `outputs`
- 후처리된 결과(바인딩용): `detections`  
- 충돌 방지/가독성을 위해 결과 변수명을 위처럼 **일관되게** 사용하세요.
