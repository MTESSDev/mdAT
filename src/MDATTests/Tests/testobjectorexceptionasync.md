# TestObjectOrExceptionAsync

> Test jsonPath array count

## Case 1

Description

``````yaml
formulaireWebFRW1DO:
  Exception: 
    Name: Exception
    ObjectName: null
    ClassName: null
    Message: Test Crashed
    ParamName: null
expected:
  generateExpectedData: null
  verify: 
    - type: match
      allowAdditionalProperties: true
      data:
        ClassName: System.Exception
        Message: Test Crashed
``````