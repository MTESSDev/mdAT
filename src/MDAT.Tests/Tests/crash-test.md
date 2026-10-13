# Crash_Test

> C# : [MarkdownTestAttributeTests.cs:194](../MarkdownTestAttributeTests.cs#L194) <!-- mdat:MarkdownTestAttributeTests.cs#MarkdownTestAttributeTests.Crash_Test -->

> Exception test

## Case 1

Description

``````yaml
val1: 1
val2: 2
expected:
  verify:
  - type: match
    jsonPath: $
    allowAdditionalProperties: true
    data: 
      ClassName: System.ArgumentOutOfRangeException
      Message: out of range!
``````