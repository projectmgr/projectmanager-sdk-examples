import * as process from 'process';
import * as projectmanager from 'projectmanager-sdk';

// Fetch the API key from an environment variable
console.log('Checking environment variables for ProjectManager API key.');
var apiKey = process.env["PM_API_KEY"] || "Fill in your API key here";
if (!apiKey) {
    console.log('Please specify an API key in the environment variable PM_API_KEY.');
    process.exit();
}

// Create a client
var client = projectmanager.ProjectManagerClient
    .withEnvironment("production")
    .withApiKey(apiKey);

async function DemonstrationMethod()
{
    // Check that we are connected
    var result = await client.Me.retrieveMe();
    console.log(`We are connected as ${result.data?.fullName} (${result.data?.emailAddress}`);
}

DemonstrationMethod();