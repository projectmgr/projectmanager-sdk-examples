package com.example;

import com.projectmanager.ProjectManagerClient;

/**
 * Starter class for MyStats application.
 *
 * @author janbodnar
 */

public class ExampleApp {

    /**
     * Application entry point.
     *
     * @param args application command line arguments
     */
    public static void main(String[] args) {
        System.out.println("Hello, world");

        // Retrieve API key from the environment variable
        String key = System.getenv().getOrDefault("PM_API_KEY", null);

        // Construct a client to talk to the ProjectManager API
        ProjectManagerClient client = ProjectManagerClient
            .withEnvironment("production")
            .withBearerToken(key);

        // Fetch me
        var result = client.getMeClient().retrieveMe();
        if (result.getSuccess()) {
            System.out.println("You are logged on as " + result.getData().getFullName() + " (" + result.getData().getEmailAddress() + ")");
        } else {
            System.out.println("Failed to connect to the server.  Check your PM_API_KEY environment variable.");
        }
    }
}