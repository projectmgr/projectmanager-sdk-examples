import process from 'process';
import { ProjectManagerClient } from 'projectmanager-sdk';

async function DemonstrationMethod()
{
    // Fetch the API key from an environment variable
    console.log('Checking environment variables for ProjectManager API key.');
    var token = process.env["PM_API_KEY"] || "Fill in your API key here";
    if (!token) {
        console.log('Please specify an API key in the environment variable PM_API_KEY.');
        process.exit();
    }

    // Create a client
    var client = ProjectManagerClient
        .withEnvironment("production")
        .withBearerToken(token);

    // Check that we are connected
    console.log('About to call retrieve me');
    var result = await client.Me.retrieveMe();
    if (result.success) {
        console.log(`We are connected as ${result.data?.fullName} (${result.data?.emailAddress})`);
    } else {
        console.log('Unable to connect to server OR not authorized');
        console.log(`Error is: ${result.error}`);
        process.abort();
    }

    // Fetch tasks
    var tasks = await client.Task.queryTasks(null, null, null, null, null, null);
    tasks.data.forEach(task => {
        console.log(`Task ${task.shortId} - ${task.name}`);
    });
}

DemonstrationMethod()
    .then(result => console.log(`Finished: ${result}`));
