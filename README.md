# job-service

# Setup

Install .Net Core 2.1+

`dotnet run`

## Routes

### Exchanges
- ```
    POST /exchanges with {
      "name": "tasks"
    } returning 
      201 => id
  ```

- ```
    GET /exchanges/{id | name} returning
      200 => json
        { "id": "27878eba-81a6-4b6f-91e0-660973a25d48",
          "name": "tasks",
          "jobCount": 0
        }
  ```

### Jobs

Note: The `body` needs to be base-64 encoded bytes.

  - ```
      POST /jobs with {
        "exchange": "27878eba-81a6-4b6f-91e0-660973a25d48",
        "subject": "requested",
        "contentType": "application/json",
        "body": ""
      } returning
        201 => id
    ```

  - ```
      GET /jobs?exchange={exchange.{id | name}}&subject={regex-like} returning
        200 => [job]
          [{"id":"2c555a3c-dbf2-419d-8bd9-33a49fa42822","createdOn":"2018-03-25T03:01:00.025991","dueBy":"0001-01-01T00:00:00","priority":0,"acceptedOn":null,"updatedOn":null,"completedOn":null,"exchange":"27878eba-81a6-4b6f-91e0-660973a25d48","subject":"requested","contentType":"application/json","body":""}]
    ```

### Reports

  - ```
      POST /jobs/{job id}/reports with {
        "title": "started",
        "body": ""
      } returning
        201 => report id
    ```

  - ```
      GET /jobs/{job id}/reports returning
        200 => [report]
          [{"page":1,"createdOn":"2018-03-25T03:14:13.357777","title":"started","body":""}]
    ```