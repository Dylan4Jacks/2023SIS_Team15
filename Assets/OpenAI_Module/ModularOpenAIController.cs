using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using System.Linq;
using System.Threading;
using UnityEngine.Networking;
using System.Collections;
using System.Net.Http;
using UnityEditor.Tilemaps;
using UnityEditor.Build.Content;
using UnityEditor.PackageManager;

public class ModularOpenAIController
{
    private OpenAIAPI api;
    private ModuleConfigGetterSetter moduleConfigGetterSetter;
    private MonoBehaviour monoBehaviour;

    // REGEX Expression for card creation

    string rxCardNameString = @"(?<=([0-9]. )).*(?=(:))";
    // New Regex Card Name String (?<=([0-9]. )).*(?=(:))
    // Old Regex (?<=([0-9]. )).*(?=(: Description:))
    string rxDescriptionString = @"(?<=(Description: )).*";
    // New Regext Description String (?<=(Description: )).*
    // Old Regex (?<=(: Description: )).*(?=(, HP:))
    string rxHPString = @"(?<=(HP: )).*";
    // New Regex Description String (?<=(HP: )).*
    // Old Regex (?<=(: HP: )).*(?=(, Speed:))
    string rxSpeedString = @"(?<=(Speed: )).*";
    // New Regex Speed String (?<=(Speed: )).*
    // Old Regex (?<=(, Speed: )).*(?=(, Attack: ))
    string rxAttackString = @"(?<=(Attack: )).*";
    // New Regex Attack String (?<=(Attack: )).*
    // Old Regex (?<=(, Attack: )).*(?=(\n)?)

    public ModularOpenAIController(MonoBehaviour mono){
        monoBehaviour = mono;
        moduleConfigGetterSetter = new ModuleConfigGetterSetter{
            NumberOfObjcets = 8,
            NumberOfObjectAttributes = 5,
            TokenLimit = 1000,
            ObjectAttributes = "Name, Description, HP (Max 20, Min 10), Speed (Max 20, Min 1), and Attack (Max 10, Min 1)",
            ObjectContextDescription = "cards in a card game",
            APIKey = "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"
        };
    }
    //Need to be a list because multiple Requests to the API will be made

    // Start is called before the first frame update
    public IEnumerator submitCharacterPrompt(string inputPrompt, List<BaseCard> Cards)
    {
        Debug.Log("Running submitCharacterPrompt with input:\n" + inputPrompt);

        List<ChatMessage> cardCreationMessage = new List<ChatMessage> { 
            //This is where the prompt limits are imput
            new (ChatMessageRole.System, "You are to create exactly" + moduleConfigGetterSetter.NumberOfObjcets + " entities related to the character brief that is given, you cannot create more or less. The entities could be people, objects or creatures. These entities will be used for " + moduleConfigGetterSetter.ObjectContextDescription + ". You will respond with only the entities " + moduleConfigGetterSetter.ObjectAttributes + " stats, no other information. The format for each entity should be numbered list similar to this '1. Lizard Frog:\n Description: This creatures lives underground and has scaly skin\n HP: 10\n Speed: 10\n Attack: 10' then double new line to create a gap between objects")
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
        List<string> cardUnserialized = new List<string>();

        // First API request
        yield return monoBehaviour.StartCoroutine(CardsRequest(cardCreationMessage, cardUnserialized));



        

        // adds each card name to a string
        string cardNames = "";
        foreach (var item in cardUnserialized)
        {
            cardNames += Regex.Match(item, rxCardNameString) + ", ";
        }

        // removes final comma and space
        cardNames = cardNames.Substring(0, cardNames.Length - 2);
            string alloactedImages = "";
            Debug.Log("END ALL");
        //string alloactedImages = await allocateImages(cardNames);

        //Initialize Array of Card Objects
        int i = 0;
        try{
            foreach (var item in cardUnserialized)
            {
                Match nameMatch = Regex.Match(item, rxCardNameString);
                Match descriptionMatch = Regex.Match(item, rxDescriptionString);
                Match hpMatch = Regex.Match(item, rxHPString);
                Match speedMatch = Regex.Match(item, rxSpeedString);
                Match attackMatch = Regex.Match(item, rxAttackString);
                Match imageMatch = Regex.Match(alloactedImages, @"(?<=(" + nameMatch.Value + ": )).*");
                BaseCard card = new BaseCard(
                                    nameMatch.Value,
                                    descriptionMatch.Value,
                                    int.Parse(attackMatch.Value), 
                                    int.Parse(speedMatch.Value), 
                                    int.Parse(hpMatch.Value),
                                    imageMatch.Value
                                    );
                Cards.Add(card);
                i++;
            }
        } catch {
            Cards = new List<BaseCard>();
        }
    }

    private IEnumerator CardsRequest(List<ChatMessage> cardCreationMessage, List<string> outputList){
        // Set up the request
        UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
        request.SetRequestHeader("Authorization", "Bearer " + moduleConfigGetterSetter.APIKey);
        request.SetRequestHeader("Content-Type", "application/json");
        
        // Constructing the request body
        var data = new
        {
            model = "gpt-3.5-turbo",
            messages = cardCreationMessage,
            temperature = 0.1f
        };
        // Convert the data object to JSON
        string dataJson = JsonConvert.SerializeObject(data);
        Debug.Log("DATABELOW");
        Debug.Log("DATA\n" + dataJson);

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(dataJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Send the request
        yield return request.SendWebRequest();

        // Handle the response
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Request Error: {request.error}");
        }
        else
        {
            string responseBody = request.downloadHandler.text;
            // Deserialize and process the response
            // Assuming you have a corresponding C# class for the response, or use dynamic with Newtonsoft.Json
            // For example, using JsonUtility:
            // ChatCompletion responseObj = JsonUtility.FromJson<ChatCompletion>(responseBody);
            // string messageContent = responseObj.choices[0].message.content;
            
            // Or, if using Newtonsoft.Json:
            Debug.Log("Just before Deserialisation\n\n");
            string adjustedJson = request.downloadHandler.text.Replace("\"object\":", "\"objectType\":");
            ChatCompletion chatCompletion = JsonUtility.FromJson<ChatCompletion>(adjustedJson);
            var messageContent = chatCompletion.choices[0].message.content;
            Debug.Log("Reached the message content success\n\n" + messageContent);
        }
    }

    [Serializable]
    public class ChatCompletion
    {
        public string id;
        public string objectType;
        public int created;
        public string model;
        public List<Choice> choices;
        public Usage usage;
    }

    [Serializable]
    public class Choice
    {
        public int index;
        public Message message;
        public string finish_reason;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

        // Send the request
    private async Task<string> allocateImages(string cardNames)
    {
        string imageOptions = "wug, beast, humanoid, furniture";
        string imageAllocationPrompt = "The following items are playing cards in a card game: " + cardNames + ". The following items are descriptive words: " + imageOptions + ". You are responsible for allocating one, and only one, of the provided descriptive words to each of the provided cards. Format your response in the following way 'card name: descriptive word' and then start a new line.";

        // Fill the user message form the input field
        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = imageAllocationPrompt;

        List<ChatMessage> test = new List<ChatMessage>();
        test.Add(userMessage);

        // Send Character creation message to OpenAI to get the reponse in cards
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 0.1,
            MaxTokens = 300,
            Messages = test
        });

        // Get Response from API
        string apiResponseString = chatResult.Choices[0].Message.Content;

        return apiResponseString;
    }

    internal static void submitCharacterPrompt()
    {
        throw new NotImplementedException();
    }
}

public class ModuleConfigGetterSetter {
    public int NumberOfObjcets { get; set; }
    public int NumberOfObjectAttributes { get; set; }
    public int TokenLimit { get; set; }
    public string ObjectAttributes { get; set; }
    public string ObjectContextDescription { get; set; }
    public string APIKey { get; set; }

}