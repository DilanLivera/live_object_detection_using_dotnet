# Live object detection using .NET

A web application to detect objects live using .NET.

## Design

The following is an initial design that was generated using Gen AI.

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
