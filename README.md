# Live object detection using .NET

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

## Where to download the models?

1. **ONNX Model Zoo**  
   The official ONNX Model Zoo is a collection of pre-trained models for various tasks, including object detection. You can find several detection models that have been converted to ONNX here.  
   [ONNX Model Zoo](https://github.com/onnx/models/tree/main/vision/object_detection_segmentation)

2. **Ultralytics YOLOv5**  
   YOLOv5 from Ultralytics is one of the most popular object detection models. Although it’s primarily developed in PyTorch, you can export the model to ONNX. You can either download the model checkpoint from their GitHub repository and convert it yourself or look for community versions already exported.  
   [Ultralytics YOLOv5 GitHub](https://github.com/ultralytics/yolov5)

3. **Other Public Repositories**  
   Repositories like [Model Zoo for ONNX](https://github.com/onnx/models) often include additional object detection models (such as SSD or Faster R-CNN variants) available in ONNX format.
