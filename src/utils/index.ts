import bcrypt from "bcryptjs"; // Import bcryptjs for password hashing and comparison

// Custom error class extending the built-in Error class
class ApiError extends Error {
    statusCode: number;  // HTTP status code
    isOperational: boolean;  // Indicates if the error is expected (operational)

    constructor(
        statusCode: number, // HTTP status code (e.g., 400, 500)
        message: string | undefined, // Error message
        isOperational = true, // Defaults to true, indicating an operational error
        stack = "" // Stack trace (optional)
    ) {
        super(message); // Call the parent constructor with the error message
        this.statusCode = statusCode;
        this.isOperational = isOperational;

        // Set the stack trace if provided, otherwise capture the stack trace
        if (stack) {
            this.stack = stack;
        } else {
            Error.captureStackTrace(this, this.constructor);
        }
    }
}

// Function to hash a password asynchronously
const encryptPassword = async (password: string) => {
    const encryptedPassword = await bcrypt.hash(password, 12); // Hash password with a salt round of 12
    return encryptedPassword; // Return the hashed password
};

// Function to compare a given password with a stored hashed password
const isPasswordMatch = async (password: string, userPassword: string) => {
    const result = await bcrypt.compare(password, userPassword); // Compare plaintext and hashed password
    return result; // Returns true if passwords match, false otherwise
};

// Exporting the custom error class and functions for use in other modules
export { ApiError, encryptPassword, isPasswordMatch };
