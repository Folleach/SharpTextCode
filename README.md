# SharpTextCode
**Bot for social network VKontakte. Which executes the C# code received in the message.**

<details><summary>Screenshots</summary><p>

![Screenshot1](https://github.com/Folleach/SharpTextCode/raw/master/Images/Screenshot1.png)
![Screenshot2](https://github.com/Folleach/SharpTextCode/raw/master/Images/Screenshot2.png)
![Screenshot3](https://github.com/Folleach/SharpTextCode/raw/master/Images/Screenshot3.png)
</p></details>

### Install
1. Create a group in VKontakte.
2. Create access key (Token) with access to messages in the group.
3. Create Callback API server in the group.
    - API version: 5.87
    - URL: {Your domain name}/api/code
4. Clone this repository.
5. Edit file "SharpTextCode/Controllers/CodeController.cs".
    - Edit constant "Token": Created access key from item 2.
    - Edit constant "VerifyCode": The generated string should return the server.
    - Add your page id to whitelist array.
    ```cs
    static void InitializeWhiteList()
    {
        WhiteList = new List<uint>
        {
            123, //Added id 123
        };
    }
    ```
6. Publish code on the server.
7. Done!
### Commands
- **FE**<br>
  Execution of code inside the method.<br>
  There is Write() method for generating a message.<br>
  **Example:**
  ```
  FE
  for (int i = 0; i < 10; i++)
  Write(i);
  Write("\nDone!");
  ```
  **Returned message:**
  ```
  012356789
  Done!
  ```
- **FF**<br>
  Complete code execution.<br>
  **Example:**
  ```
  FF
  using System; 
  class Executor 
  { 
      public static string Execute() 
      {
          string[] array = new string[] { "Hello", "user" };
          string result = "";
          foreach (string element in array)
              result += $"{element}\n";
          return result;
      } 
  }
  ```
  **Returned message:**
  ```
  Hello
  user
  ```
- **WG**<br>
  Get the whitelist.<br>
  **Example:**
  ```
  WG
  ```
  **Returned message:**
  ```
  123
  321
  9851
  ```
- **WA**<br>
  Adds a user id to the whitelist.<br>
  **Example:**
  ```
  WA 1234
  ```
- **WR**<br>
  Removes user id from the whitelist.<br>
  **Example:**
  ```
  WR 1234
  ```

I made this program just to play.
### Special thanks:
**PhoneGuy** - For assistance in testing the program.
