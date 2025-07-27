# Test Case Templates and Examples

## Test Case Template

### Test Case ID: TC-XXX-YYY
- **Component**: [Service/Tool/Feature being tested]
- **Test Type**: [Unit/Integration/E2E/Performance/Security]
- **Priority**: [High/Medium/Low]
- **Prerequisites**: [Setup requirements]
- **Test Data**: [Required data setup]

### Test Description
[Brief description of what is being tested]

### Test Steps
1. [Step 1]
2. [Step 2]
3. [Step 3]

### Expected Results
[What should happen]

### Actual Results
[What actually happened - filled during execution]

### Pass/Fail Criteria
[Specific criteria for determining success]

---

## Core Service Test Cases

### TaskService Test Cases

#### TC-TASK-001: Create Task with Valid Data
- **Component**: TaskService.CreateTaskAsync
- **Test Type**: Unit
- **Priority**: High
- **Prerequisites**: Database initialized

**Test Description**: Verify that a task can be created with valid input parameters

**Test Steps**:
1. Call CreateTaskAsync with valid title "Test Task"
2. Provide valid description "Test Description"
3. Set list_id to existing list (ID: 1)
4. Set status to Pending

**Expected Results**:
- Task is created successfully
- Task ID is returned
- Task contains correct data
- CreatedAt and UpdatedAt timestamps are set

**Pass/Fail Criteria**: Task creation succeeds and returns TaskItem with correct properties

---

#### TC-TASK-002: Create Task with Empty Title
- **Component**: TaskService.CreateTaskAsync
- **Test Type**: Unit
- **Priority**: High
- **Prerequisites**: Database initialized

**Test Description**: Verify that creating a task with empty title throws ArgumentException

**Test Steps**:
1. Call CreateTaskAsync with empty string title ""
2. Provide valid description
3. Set valid list_id

**Expected Results**:
- ArgumentException is thrown
- Exception message contains "Task title cannot be empty"
- No task is created in database

**Pass/Fail Criteria**: ArgumentException is thrown with correct message

---

#### TC-TASK-003: Update Task Status to InProgress
- **Component**: TaskService.UpdateTaskAsync
- **Test Type**: Unit
- **Priority**: High
- **Prerequisites**: 
  - Database initialized
  - Existing task in Pending status
  - Another task in InProgress status in same list

**Test Description**: Verify that setting task status to InProgress pauses other active tasks in the same list

**Test Steps**:
1. Create two tasks in same list: TaskA (Pending), TaskB (InProgress)
2. Update TaskA status to InProgress
3. Verify TaskB status changes to Pending

**Expected Results**:
- TaskA status becomes InProgress
- TaskB status becomes Pending
- Only one task is InProgress per list

**Pass/Fail Criteria**: Business rule enforced - only one active task per list

---

### ListService Test Cases

#### TC-LIST-001: Create List with Valid Data
- **Component**: ListService.CreateListAsync
- **Test Type**: Unit
- **Priority**: High

**Test Description**: Verify list creation with valid parameters

**Test Steps**:
1. Call CreateListAsync with name "Test List"
2. Provide description "Test Description"
3. Set parent_list_id to null (root level)

**Expected Results**:
- List created successfully
- List ID returned
- Properties set correctly
- Timestamps populated

**Pass/Fail Criteria**: List creation succeeds with correct data

---

#### TC-LIST-002: Prevent Circular Reference
- **Component**: ListService.UpdateListAsync
- **Test Type**: Unit
- **Priority**: High
- **Prerequisites**: Hierarchical list structure exists

**Test Description**: Verify that circular references are prevented in list hierarchy

**Test Steps**:
1. Create parent list (ID: 1)
2. Create child list (ID: 2, parent: 1)
3. Create grandchild list (ID: 3, parent: 2)
4. Attempt to set parent list's parent to grandchild (circular reference)

**Expected Results**:
- InvalidOperationException thrown
- Exception message indicates circular reference
- Hierarchy remains unchanged

**Pass/Fail Criteria**: Circular reference is detected and prevented

---

#### TC-LIST-003: Delete List with Cascade
- **Component**: ListService.DeleteListAsync
- **Test Type**: Integration
- **Priority**: Medium
- **Prerequisites**: List with child lists and tasks exists

**Test Description**: Verify cascade delete removes child lists and orphans tasks

**Test Steps**:
1. Create parent list with 2 child lists
2. Add tasks to parent and child lists
3. Call DeleteListAsync with cascadeDelete = true
4. Verify all child lists are deleted
5. Verify tasks are orphaned (list_id = null)

**Expected Results**:
- Parent list deleted
- All child lists deleted
- Tasks have list_id set to null
- Operation completes in transaction

**Pass/Fail Criteria**: Cascade delete completes successfully

---

### TemplateService Test Cases

#### TC-TEMPLATE-001: Create Template from List
- **Component**: TemplateService.CreateTemplateAsync
- **Test Type**: Unit
- **Priority**: Medium
- **Prerequisites**: List with tasks exists

**Test Description**: Verify template creation strips state from source list

**Test Steps**:
1. Create list with tasks having various statuses
2. Set some tasks as completed with completion dates
3. Create template from this list
4. Verify template tasks have Pending status
5. Verify completion dates are removed

**Expected Results**:
- Template created successfully
- All template tasks have Pending status
- State-specific data removed
- Structure preserved

**Pass/Fail Criteria**: Template contains clean task structure without state

---

## MCP Tool Test Cases

### Task Management Tools

#### TC-MCP-001: CreateTaskTool Success
- **Component**: CreateTaskTool
- **Test Type**: Integration
- **Priority**: High

**Test Description**: Verify MCP CreateTaskTool creates task correctly

**MCP Request**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "create_task",
    "arguments": {
      "title": "Test Task",
      "description": "Test Description",
      "listId": 1
    }
  }
}
```

**Expected Response**:
```json
{
  "content": [
    {
      "type": "text",
      "text": "✅ Task created successfully!\n\n**Task Details:**\n- ID: 1\n- Title: Test Task\n- Description: Test Description\n- Status: Pending\n- List: Default List\n- Created: 2025-01-26T..."
    }
  ]
}
```

**Pass/Fail Criteria**: Task created and proper MCP response returned

---

#### TC-MCP-002: CreateTaskTool Validation Error
- **Component**: CreateTaskTool
- **Test Type**: Integration
- **Priority**: High

**Test Description**: Verify MCP tool handles validation errors properly

**MCP Request**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "create_task",
    "arguments": {
      "title": "",
      "listId": 999
    }
  }
}
```

**Expected Response**:
```json
{
  "isError": true,
  "content": [
    {
      "type": "text",
      "text": "❌ Error creating task: Task title cannot be empty"
    }
  ]
}
```

**Pass/Fail Criteria**: Validation error properly returned through MCP

---

### List Management Tools

#### TC-MCP-003: ListAllListsTool Hierarchical
- **Component**: ListAllListsTool
- **Test Type**: Integration
- **Priority**: Medium
- **Prerequisites**: Hierarchical list structure exists

**Test Description**: Verify hierarchical list display through MCP

**MCP Request**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "list_all_lists",
    "arguments": {
      "hierarchical": true
    }
  }
}
```

**Expected Response**: Properly formatted hierarchical structure with indentation

**Pass/Fail Criteria**: Hierarchy correctly displayed in MCP response

---

## Database Test Cases

### Transaction Test Cases

#### TC-DB-001: Transaction Rollback on Error
- **Component**: DatabaseManager
- **Test Type**: Integration
- **Priority**: High

**Test Description**: Verify database rollback on operation failure

**Test Steps**:
1. Begin transaction
2. Create task successfully
3. Attempt invalid operation (duplicate constraint violation)
4. Verify transaction is rolled back
5. Verify initial task creation is undone

**Expected Results**:
- Transaction rolls back completely
- Database state unchanged
- No partial data commits

**Pass/Fail Criteria**: Complete rollback on error

---

#### TC-DB-002: Concurrent Access Handling
- **Component**: Database Connection Pool
- **Test Type**: Performance
- **Priority**: Medium

**Test Description**: Verify database handles concurrent operations

**Test Steps**:
1. Launch 10 concurrent task creation operations
2. Launch 5 concurrent list operations
3. Launch 3 concurrent search operations
4. Monitor for deadlocks or connection issues

**Expected Results**:
- All operations complete successfully
- No connection pool exhaustion
- No deadlocks occur
- Response times remain acceptable

**Pass/Fail Criteria**: All concurrent operations succeed

---

## Performance Test Cases

#### TC-PERF-001: Bulk Task Creation
- **Component**: TaskService
- **Test Type**: Performance
- **Priority**: Medium

**Test Description**: Verify performance of creating many tasks

**Test Steps**:
1. Create 1000 tasks in batches of 100
2. Measure total execution time
3. Monitor memory usage
4. Verify all tasks created correctly

**Expected Results**:
- Completes within 30 seconds
- Memory usage remains stable
- All 1000 tasks created

**Pass/Fail Criteria**: Meets performance benchmarks

---

#### TC-PERF-002: Search Performance
- **Component**: SearchService
- **Test Type**: Performance
- **Priority**: Medium
- **Prerequisites**: Database with 10,000 tasks

**Test Description**: Verify search performance with large dataset

**Test Steps**:
1. Execute text search across 10,000 tasks
2. Execute filtered search with multiple criteria
3. Execute tag-based search
4. Measure response times

**Expected Results**:
- Text search completes under 500ms
- Filtered search completes under 1000ms
- Tag search completes under 300ms

**Pass/Fail Criteria**: All searches meet response time targets

---

## Security Test Cases

#### TC-SEC-001: SQL Injection Prevention
- **Component**: All database operations
- **Test Type**: Security
- **Priority**: High

**Test Description**: Verify SQL injection attacks are prevented

**Test Steps**:
1. Attempt task creation with SQL injection in title: `'; DROP TABLE tasks; --`
2. Attempt list search with malicious query
3. Verify operations fail safely
4. Verify database integrity maintained

**Expected Results**:
- Malicious input treated as literal text
- No SQL commands executed
- Database structure unchanged

**Pass/Fail Criteria**: No SQL injection possible

---

#### TC-SEC-002: Input Validation
- **Component**: All MCP tools
- **Test Type**: Security
- **Priority**: High

**Test Description**: Verify input validation prevents malicious data

**Test Steps**:
1. Send oversized string inputs
2. Send invalid data types
3. Send null/undefined values
4. Send malformed JSON

**Expected Results**:
- Proper validation errors returned
- No system crashes
- No data corruption

**Pass/Fail Criteria**: All invalid inputs properly rejected

---

## Docker Test Cases

#### TC-DOCKER-001: Container Health Check
- **Component**: Docker Container
- **Test Type**: Integration
- **Priority**: High

**Test Description**: Verify container health endpoint works

**Test Steps**:
1. Start container with docker-compose
2. Wait for startup completion
3. Call health endpoint: GET /health
4. Verify response indicates healthy status

**Expected Results**:
```json
{
  "status": "Healthy",
  "database": "Connected",
  "timestamp": "2025-01-26T..."
}
```

**Pass/Fail Criteria**: Health check returns healthy status

---

#### TC-DOCKER-002: Data Persistence
- **Component**: Docker Volume Mapping
- **Test Type**: Integration
- **Priority**: High

**Test Description**: Verify data persists across container restarts

**Test Steps**:
1. Start container and create test data
2. Stop container
3. Restart container
4. Verify test data still exists

**Expected Results**:
- All data preserved
- Database file persisted
- Application functions normally

**Pass/Fail Criteria**: Data survives container restart

---

## Test Execution Checklist

### Pre-Execution Setup
- [ ] Test environment prepared
- [ ] Database reset to clean state
- [ ] Test data loaded if required
- [ ] Mock services configured
- [ ] Logging enabled

### During Execution
- [ ] Document actual results
- [ ] Capture screenshots/logs for failures
- [ ] Note performance metrics
- [ ] Record any anomalies

### Post-Execution
- [ ] Clean up test data
- [ ] Reset environment state
- [ ] Update test results
- [ ] Report defects if found
- [ ] Update coverage metrics

---

These test cases provide concrete examples of how to validate the Task List MCP Server functionality across all components and scenarios. Each test case includes specific inputs, expected outputs, and clear pass/fail criteria for consistent execution and reporting.
