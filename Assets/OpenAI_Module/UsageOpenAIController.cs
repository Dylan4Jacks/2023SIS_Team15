using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using System.Linq;
using System.Threading;

public class UsageOpenAIController: MonoBehaviour
{
    private OpenAIAPI api;
    private UsageConfigGetterSetter usageConfigGetterSetter;
    public TMP_Text outputText;
    public TMP_InputField inputUsageField;
    public Button BtnSubmit;

    public UsageOpenAIController(){
        string configJsonString  = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/OpenAI_Module/moduleConfig.json"));
        usageConfigGetterSetter = JsonConvert.DeserializeObject<UsageConfigGetterSetter>(configJsonString)!;
    }
    //Need to be a list because multiple Requests to the API will be made

    void Start()
    {
        BtnSubmit.onClick.AddListener(() => processUsage());
    }

    void processUsage(){

        Task<string> taskPlayer = submitCharacterPrompt(inputUsageField.text);

        System.Threading.Tasks.Task.WhenAll(taskPlayer).ContinueWith(allTasks => {
            //LoadingScene(false);
            if (allTasks.Status == TaskStatus.RanToCompletion) {
                string cards = taskPlayer.Result;
                Debug.Log("This is the last debug: " + cards);
                outputText.text = cards;
            }
        });

    }

    // Start is called before the first frame update
    public Task<string> submitCharacterPrompt(string inputPrompt)
    {
        //Create a new instance of the OpenAI API, and give it the APIKEY (Stored in the System Environment Variables)
        string API_KEY = usageConfigGetterSetter.APIKey;; 
        if(API_KEY == "" || API_KEY == null){
            try{
                API_KEY = Environment.GetEnvironmentVariable("OPEN_AI_APIKEY", EnvironmentVariableTarget.User);
                if(API_KEY == null){
                    API_KEY = usageConfigGetterSetter.APIKey;
                }
            }catch(Exception e){
                Debug.LogError(e);
            }
        }
        api = new OpenAIAPI(API_KEY);
        return StartCharacterCreation(inputPrompt);
    }
    private Task<string> StartCharacterCreation(string inputPrompt)
    {
        Task<string> task = Task.Run(() =>
        {
            return GetResponse(inputPrompt);
        });
        return task;
    }

    private async Task<string> GetResponse(string inputPrompt)
    {
        List<ChatMessage> cardCreationMessage = new List<ChatMessage> { 
            //This is where the prompt limits are imput
            new (ChatMessageRole.System, "You are to create exactly" + usageConfigGetterSetter.NumberOfObjcets + " entities related to the brief that is given, you cannot create more or less. The entities could be people, objects or creatures. These entities will be used for " + usageConfigGetterSetter.ObjectContextDescription + ". You will respond with only the entities " + usageConfigGetterSetter.ObjectAttributes + " stats, no other information. The format for each entity should be numbered list similar to this '1. Lizard Frog:\n Description: This creatures lives underground and has scaly skin\n HP: 10\n Speed: 10\n Attack: 10' then double new line to create a gap between objects")
            // Example Brief: The character brief is: I am a noble knight. I was born in a little village and conscripted into the royal army for training at a young age. I fight with sword and shield honourably to protect the king's palace.
        };

        // Fill the user message form the input field
        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = inputPrompt;
        if (userMessage.Content.Length > 200) //This is here to save tokens when making an API
        {
            //Shorten user message if over 100
            userMessage.Content = userMessage.Content.Substring(0, 200);
        }

        //Add Message to list
        cardCreationMessage.Add(userMessage);


        // Send Character creation message to OpenAI to get the reponse in cards
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 0.1,
            MaxTokens = usageConfigGetterSetter.TokenLimit,
            Messages = cardCreationMessage
        });

        // Get Response from API
        ChatMessage APIResponse = new ChatMessage();
        APIResponse.Role = chatResult.Choices[0].Message.Role;
        APIResponse.Content = chatResult.Choices[0].Message.Content;
        
        cardCreationMessage.Add(APIResponse);

        string apiResponseString = APIResponse.Content;


        return apiResponseString;
    }

    internal static void submitCharacterPrompt()
    {
        throw new NotImplementedException();
    }
}
public class UsageConfigGetterSetter {
    public int NumberOfObjcets { get; set; }
    public int NumberOfObjectAttributes { get; set; }
    public int TokenLimit { get; set; }
    public string ObjectAttributes { get; set; }
    public string ObjectContextDescription { get; set; }
    public string APIKey { get; set; }

}