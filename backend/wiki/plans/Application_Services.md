# Application Services (API Contracts) - Detailed UI Specification

This document defines the **Application Layer** (Use Cases) for the Distributed Examination System. It maps directly to ABP `ApplicationService` interfaces and is designed to support the specific needs of the Student UI, Proctor Dashboard, and Central Admin Console.

---

## 1. Shared Contracts (`MyProject.Domain.Shared`)

### 1.1 `ExamPackageDto` (The Sealed Envelope)
*   **Purpose:** The complete data blob downloaded by the Local Server.
*   **Structure:**
    ```csharp
    public class ExamPackageDto
    {
        public Guid ExamInstanceId { get; set; }
        public string Title { get; set; }
        public int DataVersion { get; set; }
        public TimeSpan Duration { get; set; }
        public ExamNavigationMode NavigationMode { get; set; } // Linear vs Free
        
        // Grouped by Section
        public List<ExamSectionDto> Sections { get; set; }
    }
    
    public class ExamSectionDto 
    {
        public string Title { get; set; }
        public List<QuestionDto> Questions { get; set; }
    }
    
    public class QuestionDto 
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public double Points { get; set; }
        public QuestionType Type { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public List<string> Tags { get; set; }
        public List<QuestionMediaDto> Medias { get; set; } // Multimedia
        public List<OptionDto> Options { get; set; }
    }

    public class QuestionMediaDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public MediaType Type { get; set; }
        public string MimeType { get; set; }
    }

    public class ResumeExamDto
    {
        public ExamPackageDto ExamPackage { get; set; }
        public List<StudentAnswerDto> SavedAnswers { get; set; }
        public DateTime ResumedAt { get; set; }
    }
    ```

### 1.2 `ExamResultDto` (The Report Card)
*   **Purpose:** The result payload uploaded to Central.
*   **Structure:**
    ```csharp
    public class OptionDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        // Note: IsCorrect is deliberately EXCLUDED from this DTO.
        // Grading happens server-side or via encrypted blob not exposed here.
    }

    public class ExamResultDto
    {
        public Guid StudentId { get; set; }
        public Guid ExamInstanceId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public List<StudentAnswerDto> Answers { get; set; }
    }
    
    // Inputs
    public class StartInput { public string UnlockCode { get; set; } }

    public class SubmitAnswerInput 
    { 
        public Guid QuestionId { get; set; } 
        public QuestionType Type { get; set; }
        public Guid? SelectedOptionId { get; set; }
        public string? TextAnswer { get; set; }
    }

    public class CreateScheduleInput 
    {
        public Guid ExamId { get; set; }
        public Guid CenterId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public AssignmentStrategyType StrategyType { get; set; }
        public string? ReferenceId { get; set; } // e.g., GroupId
        public List<Guid>? ExplicitStudentIds { get; set; } 
    }
    ```

---

## 2. Central Server APIs (`MyProject.ExamManagement`)

### 2.1 `IExamDefinitionAppService` (Authoring UI)
*   **`GetListAsync(GetExamsInput)`** -> `PagedResultDto<ExamDefinitionDto>`
    *   *UI:* "My Exams" list. Filters: Status (Draft/Published), Creator.
*   **`GetAsync(Guid id)`** -> `ExamDefinitionDetailDto`
    *   *UI:* Exam Editor Screen. Returns full structure (Sections, Rules).
*   **`CreateAsync(CreateExamInput)`**
    *   *UI:* "New Exam" wizard.
*   **`UpdateAsync(Guid id, UpdateExamInput)`**
    *   *UI:* Save changes to Draft.
*   **`PublishAsync(Guid id)`**
    *   *UI:* "Publish" button. Triggers validation and event.
*   **`ArchiveAsync(Guid id)`**
    *   *UI:* "Archive" button. Hides exam from future scheduling.

### 2.2 `IExamInstanceAppService` (Distribution)
*   **`GenerateBatchesAsync(GenerateInput)`**
    *   Input: `DefinitionId`, `TimeSlot`, `CenterId`, `Count`.
    *   *UI:* "Schedule Exam" wizard.
*   **`GetDownloadPackageAsync(Guid id)`** (Machine-to-Machine)
    *   *Auth:* `client_credentials` (Center).
    *   *Returns:* `Stream` (A ZIP file containing `exam.json` and `/assets` folder).
    *   *Strategy:* Uses `IHttpClientFactory` with `Polly` to stream large files to disk.

### 2.3 `IQuestionAppService` (Question Bank UI)
*   **`GetListAsync(GetQuestionsInput)`** -> `PagedResultDto<QuestionDto>`
    *   *UI:* Question Bank grid. Filters: Pool, Difficulty, Tags, Text Search.
*   **`CreateAsync(CreateQuestionInput)`**
    *   *UI:* "Add Question" modal.
*   **`UpdateAsync(Guid id, UpdateQuestionInput)`**
    *   *UI:* Edit Question modal.
*   **`UploadMediaAsync(Guid id, UploadMediaInput)`**
    *   *UI:* "Attach Image" button. Uploads file to Blob Storage and links to Question.
*   **`GetPoolLookupAsync()`** -> `List<QuestionPoolDto>`
    *   *UI:* Dropdown for selecting Pools.

### 2.6 `IQuestionPoolAppService` (Bank Management)
*   **`GetListAsync(GetPoolsInput)`** -> `PagedResultDto<QuestionPoolDto>`
    *   *UI:* "Question Pools" management grid.
*   **`CreateAsync(CreatePoolInput)`**
    *   *UI:* "New Pool" modal.
*   **`UpdateAsync(Guid id, UpdatePoolInput)`**
    *   *UI:* "Edit Pool" modal.
*   **`ToggleStatusAsync(Guid id)`**
    *   *UI:* "Archive/Activate" button.

### 2.7 `IExamScheduleAppService` (Time & Assignments)
*   **`CreateAsync(CreateScheduleInput)`**
    *   *UI:* "New Schedule" modal. Select: Exam, Center, TimeRange.
*   **`UpdateAsync(Guid id, UpdateScheduleInput)`**
    *   *UI:* Change time or capacity limits (only if not Published).
*   **`PublishAsync(Guid id)`**
    *   *UI:* "Publish" button. Makes it visible to students/centers.
*   **`CancelAsync(Guid id)`**
    *   *UI:* "Cancel" button. Flips status to Closed.
*   **`GetListAsync(GetSchedulesInput)`** -> `PagedResultDto<ExamScheduleDto>`
    *   *UI:* Master Calendar View. Filters: Center, Date, Exam.

### 2.4 `IExamCenterAppService` (Admin Console)
*   **`GetListAsync(GetCentersInput)`** -> `PagedResultDto<ExamCenterDto>`
    *   *UI:* "Exam Centers" grid. Columns: Name, Capacity, Status.
*   **`CreateAsync(CreateCenterInput)`**
    *   *UI:* "Register Center" form.
    *   *Returns:* `ExamCenterSecretDto` (Contains `ClientSecret` - displayed once).
*   **`UpdateAsync(Guid id, UpdateCenterInput)`**
    *   *UI:* Edit form. Update Name, Capacity, Location.
*   **`SetStatusAsync(Guid id, CenterStatus status)`**
    *   *UI:* "Maintenance Mode" toggle.
*   **`ResetCredentialsAsync(Guid id)`**
    *   *UI:* "Reset Secret" button. Returns new Secret.
*   **`GetSchedulesAsync(Guid centerId, DateRange range)`** -> `List<ExamScheduleDto>`
    *   *UI:* Calendar view of exams at a specific center.

### 2.5 `IResultAppService` (Grading UI)
*   **`GetListAsync(GetResultsInput)`** -> `PagedResultDto<ExamResultListDto>`
    *   *UI:* "Gradebook" grid. Filters: Exam, Student, Status (Pending/Graded).
*   **`GetDetailAsync(Guid id)`** -> `ExamResultDetailDto`
    *   *UI:* Student Answer Sheet view. Shows Question + Given Answer + Correct Answer.
*   **`UpdateGradeAsync(Guid id, UpdateGradeInput)`**
    *   *UI:* Manual grading for Essays (Instructor overrides score).
*   **`SubmitReviewAsync(Guid resultId, SubmitReviewInput)`**
    *   *UI:* "Request Review" button (Student Portal). Input: Reason/Comment.
*   **`GetReviewStatusAsync(Guid resultId)`** -> `ExamReviewDto`
    *   *UI:* "My Appeals" list. Shows Status and Instructor Response.
*   **`ProcessReviewAsync(Guid reviewId, ReviewDecisionInput)`**
    *   *UI:* "Appeals" dashboard. Input: Approve/Reject + Comment.

### 2.8 `IStudentAppService` (Student Management)
*   **`GetListAsync(GetStudentsInput)`** -> `PagedResultDto<StudentDto>`
    *   *UI:* "Students" grid. Filters: Group, Name.
    *   *Returns:* Student with linked user info (Name, Email from IdentityUser).
*   **`GetAsync(Guid id)`** -> `StudentDetailDto`
    *   *UI:* Student detail view.
*   **`CreateAsync(CreateStudentInput)`**
    *   *Input:* `UserId`, `GroupId?`
    *   *UI:* "Add Student" modal. Select existing user and optionally assign to group.
    *   *Validation:* User must not already be a Student or Instructor.
*   **`UpdateAsync(Guid id, UpdateStudentInput)`**
    *   *Input:* `GroupId?`
    *   *UI:* "Edit Student" modal. Change group assignment.
*   **`DeleteAsync(Guid id)`**
    *   *UI:* "Remove Student" button. Removes student record (does not delete IdentityUser).
*   **`AssignToGroupAsync(Guid studentId, Guid groupId)`**
    *   *UI:* Bulk action or drag-drop to group.
*   **`RemoveFromGroupAsync(Guid studentId)`**
    *   *UI:* "Remove from Group" button.
*   **`GetLookupAsync(GetStudentLookupInput)`** -> `List<StudentLookupDto>`
    *   *UI:* Dropdown/autocomplete for selecting students (e.g., in ExamSchedule manual assignment).

### 2.9 `IInstructorAppService` (Instructor Management)
*   **`GetListAsync(GetInstructorsInput)`** -> `PagedResultDto<InstructorDto>`
    *   *UI:* "Instructors" grid.
    *   *Returns:* Instructor with linked user info (Name, Email from IdentityUser).
*   **`GetAsync(Guid id)`** -> `InstructorDetailDto`
    *   *UI:* Instructor detail view.
*   **`CreateAsync(CreateInstructorInput)`**
    *   *Input:* `UserId`
    *   *UI:* "Add Instructor" modal. Select existing user.
    *   *Validation:* User must not already be a Student or Instructor.
*   **`DeleteAsync(Guid id)`**
    *   *UI:* "Remove Instructor" button. Removes instructor record (does not delete IdentityUser).
*   **`GetLookupAsync()`** -> `List<InstructorLookupDto>`
    *   *UI:* Dropdown for selecting grader/reviewer.

### 2.10 `IStudentGroupAppService` (Group Management)
*   **`GetListAsync(GetGroupsInput)`** -> `PagedResultDto<StudentGroupDto>`
    *   *UI:* "Student Groups" grid. Columns: Name, Faculty, Year, Student Count, Status.
*   **`GetAsync(Guid id)`** -> `StudentGroupDetailDto`
    *   *UI:* Group detail view with list of students.
*   **`CreateAsync(CreateGroupInput)`**
    *   *Input:* `Name`, `Faculty`, `Department`, `AcademicYear`
    *   *UI:* "New Group" modal.
*   **`UpdateAsync(Guid id, UpdateGroupInput)`**
    *   *UI:* "Edit Group" modal.
*   **`ToggleStatusAsync(Guid id)`**
    *   *UI:* "Archive/Activate" button.
*   **`GetStudentsAsync(Guid groupId)`** -> `List<StudentDto>`
    *   *UI:* "View Students" in group detail.
*   **`GetLookupAsync()`** -> `List<StudentGroupLookupDto>`
    *   *UI:* Dropdown for selecting group (e.g., in ExamSchedule assignment strategy).

---

## 3. Local Server APIs (`MyProject.ExamExecution`)

### 3.1 `IExamSessionAppService` (Student Exam Player)
*   **`GetActiveSessionAsync()`** -> `ExamSessionDto?`
    *   *UI:* On login, checks if student has an interrupted session.
*   **`StartExamAsync(StartInput)`**
    *   Input: `UnlockCode`.
    *   *UI:* "Enter Code" screen.
    *   *Returns:* `ExamPackageDto` (The content to render).
*   **`ResumeAsync(Guid sessionId)`** -> `ResumeExamDto`
    *   *UI:* "Resume" button on dashboard.
    *   *Logic:* Re-validates session, returns exam content + saved answers.
*   **`HeartbeatAsync(Guid sessionId)`**
    *   *UI:* Called every 30s. Updates `LastHeartbeat`.
    *   *Logic:* Used for "Offline" detection on Proctor Dashboard.
*   **`SubmitAnswerAsync(Guid sessionId, SubmitAnswerInput)`**
    *   *UI:* Background auto-save when student selects an option.
    *   *Returns:* `200 OK` (Ack).
*   **`ToggleFlagAsync(Guid sessionId, Guid questionId, bool isFlagged)`**
    *   *UI:* "Review Later" toggle on question card.
*   **`GetTimeRemainingAsync(Guid sessionId)`**
    *   *UI:* Header Timer. Syncs server time.
*   **`FinishExamAsync(Guid sessionId)`**
    *   *UI:* "Submit Exam" button.
    *   *Logic:* Validates Grace Period -> Marks Submitted -> Queues Outbox.

### 3.2 `IProctorAppService` (Supervisor Dashboard)
*   **`GetDashboardStatsAsync()`** -> `ProctorDashboardDto`
    *   *UI:* Real-time stats: "Total Students", "Active", "Submitted", "Offline Alerts".
*   **`GetStudentListAsync()`** -> `List<StudentSessionDto>`
    *   *UI:* Live grid of students in the room.
    *   Columns: Name, Status (Online/Offline), Progress (15/50 answered), Time Remaining.
*   **`OpenRoomAsync(Guid centerSessionId)`**
    *   *UI:* "Start Exam Session" button. Enables student logins.
*   **`CloseRoomAsync(Guid centerSessionId)`**
    *   *UI:* "Close Room" button. Blocks new logins.
*   **`UnlockSessionAsync(Guid sessionId)`**
    *   *UI:* "Resume" button (for PC crash recovery).
*   **`GenerateRoomCodeAsync()`**
    *   *UI:* "Show Code" button. Displays the dynamic 4-char unlock code.
*   **`GetTodaySessionsAsync()`** -> `List<ExamCenterSessionDto>`
    *   *UI:* "Select Exam" dropdown. Shows exams downloaded for today.

### 3.3 `ISyncAppService` (System Monitor)
*   **`GetSyncStatusAsync()`** -> `List<SyncRecordDto>`
    *   *UI:* "Sync Health" page. Shows pending uploads, last sync time, errors.
*   **`TriggerSyncAsync()`**
    *   *UI:* "Force Sync Now" button.
*   **`GetLocalExamsAsync()`** -> `List<DeliveredExamDto>`
    *   *UI:* "Download Status" list. Confirms readiness.

---

## 4. Integration Events (RabbitMQ)

### 4.1 `ExamPublishedEto` (Central -> Local)
*   `ExamInstanceId`, `CenterId`, `DataVersion`, `DownloadUrl`, `Checksum`.

### 4.2 `ExamSubmittedEto` (Local -> Central)
*   `StudentId`, `ExamInstanceId`, `Answers` (List), `SubmittedAt`, `DeviceFingerprint`.
