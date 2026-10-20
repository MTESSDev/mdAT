# Array_order_is_ignored_when_the_test_case_asks_for_it

> C# : [AdapterTests.cs:54](../AdapterTests.cs#L54) <!-- mdat:AdapterTests.cs#AdapterTests.Array_order_is_ignored_when_the_test_case_asks_for_it -->

## Case 1

Same values, other order: passes because `ignoreArrayOrder` is declared on the verification step.

``````yaml
values: [3, 1, 2]
items:
  - A: first
  - A: second
expected:
  verify:
    - type: match
      jsonPath: $.values
      ignoreArrayOrder: true
      data: [1, 2, 3]
    - type: match
      jsonPath: $.items
      ignoreArrayOrder: true
      allowAdditionalProperties: true
      data:
        - A: second
        - A: first
``````
