import os

# To test Python locally, first `pip uninstall projectmanagersdk` then uncomment these two lines.
# Use the path here to point to your local copy of the SDK source:
# import sys
# sys.path.append('/github/projectmanager-sdk-python/src')

import ProjectManagerSdk
import dataclasses
import json

def retrieve_api_key():
    API_KEY = os.environ.get('PM_API_KEY')
    if API_KEY is None:
        print('Your API key is not set.  Please configure the environment variable PM_API_KEY.')
        quit()
    else:
        return API_KEY


def create_client(apikey):
    env = 'production'
    client = ProjectManagerSdk.ProjectManagerClient(env, 'EXAMPLE_PYTHON_APP')
    client.with_api_key(apikey)
    if not client:
        print("Problem creating client; either missing an API key or an environment.")
    else:
        return client

def remove_empty_elements(d):
    """recursively remove empty lists, empty dicts, or None elements from a dictionary"""

    def empty(x):
        return x is None or x == {} or x == []

    if not isinstance(d, (dict, list)):
        return d
    elif isinstance(d, list):
        return [v for v in (remove_empty_elements(v) for v in d) if not empty(v)]
    else:
        return {k: v for k, v in ((k, remove_empty_elements(v)) for k, v in d.items()) if not empty(v)}

def main():
    API_KEY = retrieve_api_key()
    client = create_client(API_KEY)
    status_results = client.me.retrieve_me()
    if not status_results.success or not status_results.data:
        print("Your API key is not valid.")
        print("Please set the environment variable PM_API_KEY and PM_ENV and try again.")
        exit()
    print(f"Logged in as {status_results.data.fullName} ({status_results.data.emailAddress})")

    # What version of the SDK are we testing?
    print(f"Testing against SDK {client.sdkVersion}")

    # Demonstrate querying tasks - fetch top 10 records only
    tasks = client.task.query_tasks(10, None, None, None, None)

    if tasks.data == None or len(tasks.data) == 0:
        print("No records found matching this query.")
        exit()

    count = 0
    for task in tasks.data:
        print(f"Task {count}: {task.shortId} {task.name}")
        count += 1

    # Demonstrate creating a project
    new_project = ProjectManagerSdk.ProjectCreateDto()
    new_project.name = "New Project - Python SDK"
    new_project.description = "This is my project description"
    result = client.project.create_project(new_project)
    print(f"Result: {result}")

    # Demonstrate create many tasks
    taskList: list[ProjectManagerSdk.TaskCreateDto] = []
    taskList.append(ProjectManagerSdk.TaskCreateDto(name = "First Task", description="Description"))
    taskList.append(ProjectManagerSdk.TaskCreateDto(name = "Second Task", description="Description"))
    taskList.append(ProjectManagerSdk.TaskCreateDto(name = "Third Task", description="Description"))
    result = client.task.create_many_tasks("e349001a-6050-441f-afbf-4b79ede6c9b6", taskList)
    print(f"Result: {result}")

if __name__ == '__main__':
    main()