# ABP Project Structure: Distributed Examination System

## 1. ExamManagement

```text
/modules/MyProject.ExamManagement
├── /src
│   ├── /MyProject.ExamManagement.Domain           <-- DDD Core
│   │   ├── /ExamDefinitions                       <-- Aggregate Folder
│   │   │   ├── ExamDefinition.cs                  <-- Root
│   │   │   ├── ExamSection.cs                     <-- Entity
│   │   │   ├── ExamDefinitionManager.cs           <-- Domain Service
│   │   │   └── IExamDefinitionRepository.cs       <-- Repository Interface
│   │   │
│   │   ├── /ExamInstances                         <-- Aggregate Folder
│   │   │   ├── ExamInstance.cs
│   │   │   ├── ExamInstanceItem.cs
│   │   │   ├── VersionGenerator.cs                <-- Domain Service
│   │   │   └── IExamInstanceRepository.cs
│   │   │
│   │   ├── /QuestionBank                          <-- Aggregate Folder
│   │   │   ├── QuestionPool.cs
│   │   │   ├── Question.cs
│   │   │   └── IQuestionRepository.cs
│   │   │
│   │   └── /Scheduling                            <-- Aggregate Folder
│   │       ├── ExamSchedule.cs
│   │       ├── AssignmentType.cs                  <-- Enum (Group, Course, Manual)
│   │       ├── ExamScheduleManager.cs
│   │       └── IExamScheduleRepository.cs
│   │
│   │   ├── /ExamResults                           <-- Aggregate Folder
│   │   │   ├── ExamResult.cs                      <-- Root (Student + Score + Status)
│   │   │   ├── QuestionResult.cs                  <-- Entity (QuestionId + GivenAnswer + Score)
│   │   │   ├── ExamReview.cs                      <-- Root (Appeal Request)
│   │   │   ├── GradingService.cs                  <-- Domain Service (Auto-Grading Logic)
│   │   │   └── IExamResultRepository.cs
│   │   │
│   │   └── /ExamCenters                           <-- Aggregate Folder
│   │       ├── ExamCenter.cs                      <-- Root (Capacity + LinkedClientId)
│   │       ├── CapacityManager.cs                 <-- Domain Service (Overbooking Prevention)
│   │       └── IExamCenterRepository.cs
│   │
│   ├── /MyProject.ExamManagement.Application      <-- Use Cases
│   │   ├── /Exams
│   │   │   ├── ExamDefinitionAppService.cs
│   │   │   └── ExamInstanceAppService.cs
│   │   └── /Scheduling
│   │       └── ExamScheduleAppService.cs
│   │
│   ├── /MyProject.ExamManagement.EntityFrameworkCore
│   │   └── ExamManagementDbContext.cs             <-- PostgreSQL Config
│   │
│   └── /MyProject.ExamManagement.HttpApi          <-- API Controllers
```

---

## 2. ExamExecution

```text
/modules/MyProject.ExamExecution
├── /src
│   ├── /MyProject.ExamExecution.Domain
│   │   ├── /DeliveredExams                        <-- Aggregate Folder
│   │   │   ├── DeliveredExam.cs                   <-- Read-Only Root
│   │   │   ├── ExamSyncManager.cs                 <-- Domain Service (Downloads JSON)
│   │   │   └── IDeliveredExamRepository.cs
│   │   │
│   │   ├── /ExamSessions                          <-- Aggregate Folder
│   │   │   ├── ExamSession.cs                     <-- State Machine Root
│   │   │   ├── StudentAnswer.cs                   <-- Value Object
│   │   │   ├── ExamSessionManager.cs              <-- Domain Service (Start/Submit logic)
│   │   │   └── IExamSessionRepository.cs
│   │   │
│   │   └── /ExamCenterSessions                    <-- Aggregate Folder
│   │       ├── ExamCenterSession.cs               <-- Room Logic
│   │       ├── RoomCodeGenerator.cs               <-- Domain Service
│   │       ├── AccessControlService.cs            <-- Domain Service
│   │       └── IExamCenterSessionRepository.cs
│   │
│   ├── /MyProject.ExamExecution.Application
│   │   ├── /Sessions
│   │   │   └── ExamSessionAppService.cs           <-- Student API
│   │   └── /Sync
│   │       └── SyncWorker.cs                      <-- Background Job for Uploads
│   │
│   ├── /MyProject.ExamExecution.EntityFrameworkCore
│   │   └── ExamExecutionDbContext.cs               <-- PostgreSQL Config
```
