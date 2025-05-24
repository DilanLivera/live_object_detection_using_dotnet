# Object detection using .NET

A web application to detect objects live using .NET.

## Design

The following is an initial design that was generated using Gen AI.

### System Context Diagram

```plantuml
@startuml SystemContextDiagram
!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Context.puml

Person(user, "User", "An end-user interacting with the Object Detection system via a web browser.")
System(system, "Object Detection Web Application", "A Blazor Server application that performs real-time object detection on a live video feed.")

System_Ext(camera, "Built-in Camera", "The laptop's camera used to capture live video.")
System_Ext(db, "Detection Database", "Stores detection results (including confidence scores).")

Rel(user, system, "Uses")
Rel(system, camera, "Captures live video from", "JavaScript/MediaDevices API")
Rel(system, db, "Saves detection results to", "Entity Framework Core / SQL")
@enduml
```

![SystemC context Diagram](https://github.com/user-attachments/assets/529aa1da-95c8-437f-b444-6987352ff87a)

### Container Diagram

```plantuml
@startuml ContainerDiagram
!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Container.puml

Person(user, "User", "An end-user interacting with the web app through a browser.")

System_Boundary(webapp, "Object Detection Web Application") {
  Container(blazor, "Blazor Server App", "ASP.NET Core", "Hosts UI, SignalR for real-time updates, and orchestrates server-side processing.")
  Container(detService, "Object Detection Service", "C# / ONNX Runtime", "Processes video frames, performs object detection inference, and extracts results.")
  Container(db, "Detection Database", "SQL Server / SQLite", "Stores detection results (bounding boxes, labels, confidence scores).")
  Container(js, "Camera & UI JS Components", "JavaScript", "Accesses the built-in camera and overlays detection results onto the video feed.")
}

Rel(user, blazor, "Uses", "HTTPS/Browser")
Rel(blazor, js, "Invokes", "JavaScript Interop")
Rel(blazor, detService, "Calls to process frames", "HTTP / Method Call")
Rel(blazor, db, "Reads/Writes detection results", "Entity Framework Core")
Rel(detService, db, "Persists detection results", "Entity Framework Core")
@enduml
```

![Container Diagram](https://github.com/user-attachments/assets/3e29b48b-7bac-4d54-b614-7d6315b7941c)

### Component Diagram

```plantuml
@startuml ComponentDiagram
!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Component.puml

Container(blazor, "Blazor Server App", "ASP.NET Core", "The main application handling UI, business logic, and coordination.")

Component(videoCapture, "Video Capture Component", "JavaScript & Blazor JS Interop", "Handles accessing the built-in camera and capturing video frames.")
Component(objectDetection, "Object Detection Service", "C#", "Processes captured frames using ONNX Runtime to perform model inference.")
Component(dataPersistence, "Data Persistence", "Entity Framework Core", "Manages storage and retrieval of detection results from the database.")
Component(realTimeComms, "Real-Time Communication (SignalR Hub)", "C#", "Pushes detection results to the client UI in real time.")

Rel(videoCapture, objectDetection, "Sends captured frames for processing")
Rel(objectDetection, dataPersistence, "Persists detection results")
Rel(objectDetection, realTimeComms, "Pushes detection results via")
Rel(realTimeComms, videoCapture, "Updates the UI overlay with detection data")
@enduml
```

![Component Diagram](https://github.com/user-attachments/assets/ca6e16f1-d445-4045-bd4d-90e70270c918)

## How to run the application

### Prerequisites

1. .NET SDK: If necessary, download and install the .NET SDK from the [Download .NET](https://dotnet.microsoft.com/download) page. Please refer to the [UI.csproj](https://github.com/DilanLivera/live_object_detection_using_dotnet/blob/main/src/UI/UI.csproj) file to find the SDK version.

2. Camera Support: The application requires a webcam for video input.

### Setup

1. Clone the Repository

   ```bash
   git clone https://github.com/DilanLivera/live_object_detection_using_dotnet
   cd live_object_detection_using_dotnet
   ```

2. Download Required Files

   a. ONNX Model

    - Create a `Infrastructure/Models/TinyYoloV3` directory in the `src/UI` folder if it does not exist.
    - Download the "Tiny YOLOv3" model from the [ONNX Model](https://github.com/onnx/models/blob/main/validated/vision/object_detection_segmentation/tiny-yolov3/model/tiny-yolov3-11.onnx) page to the `src/UI/Infrastructure/Models/TinyYoloV3` directory. Please make sure the model file name is `tiny-yolov3-11.onnx`. The input and output requirements depend on the version of the model. Because of this, we can't use a model that is different from the current implementation.

   b. COCO Labels File

   The coco.names file is essential because it maps numerical class indices output by the YOLO model to human-readable labels. The YOLO v3 model can detect 80 different classes. When the model detects an object, it outputs a number (0-79) corresponding to its class. The position (line number) in coco.names match these indices exactly - for example, if the model outputs "39", the code looks up the 40th line in coco.names to find "bottle". This order must be preserved exactly as the model was trained, or objects will be mislabeled. Without this file, users would see meaningless numbers instead of object names like "person" or "car". The `coco.names` file is already included in the `src/UI/Infrastructure/Models` directory.

### Running the Application

1. Start the Application

   ```bash
   cd src/UI
   dotnet run
   ```

2. Access the Web Interface

- Open your web browser and navigate to `http://localhost:5013`
- Allow camera access when prompted by the browser

3. Start Camera

- Click the "Start Camera" button to initialize your webcam
- The video feed should appear in the main window

4. Object Detection: The application automatically detects objects once the Camera is active. It highlights the detected objects with bounding boxes. Each detection includes a label and confidence score.

### Troubleshooting

#### Model Loading Issues

- Verify that the model files are correctly placed in the `src/UI/Infrastructure/Models/TinyYoloV3` directory
- Check the application logs for any specific error messages
- Ensure the model file name match `tiny-yolov3-11.onnx`.
