import * as React from "react";
import { TextField } from "@fluentui/react";
import Progress from "./Progress";
import { useCallback, useEffect } from "react";
import { KeyboardEvent } from "react";
import { useState } from "react";
import config from "../../config/config";
import { Client } from "@microsoft/microsoft-graph-client";
import { getAccessTokenMSAL } from "../../auth/fallbackAuthTaskpane";
import moment from "moment";

interface ODataResponse<T> {
  "@odata.context": string,
  "@odata.nextLink": string,
  "value": Array<T>
}

interface CalendarReponse {
  id: string,
  subject: string,
  start: CalendarReponseDate,
  end: CalendarReponseDate
}

interface CalendarReponseDate {
  dateTime: string,
  timeZone: string
}

interface CalendarEvent {
  id: string,
  subject: string,
  start: string,
  end: string
}

async function queryOpenAi(messages: { role: string, content: string }[]) {
  const { endpoint, key, model } = config;

  const url = `${endpoint}openai/deployments/${model}/chat/completions?api-version=2023-03-15-preview`;

  const body = {
    messages: messages,
    max_tokens: 800,
    temperature: 0.7,
    frequency_penalty: 0,
    presence_penalty: 0,
    top_p: .95,
    stop: null
  };

  console.log(url);

  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "api-key": key
    },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    return null;
  }

  return await response.json();
}

function formatMessageForDisplay(content: string) {
  const marker = "***";
  const parts = [];

  let rest = content;
  let index = rest.indexOf(marker);
  let format = index === 0 ? "mail" : "chat";
  if (index === 0) {
    rest = rest.substring(index + 3);
    index = rest.indexOf(marker);
  }

  while (index > -1) {
    const part = rest.substring(0, index);

    parts.push({
      format: format,
      content: part
    });

    format = format === "mail" ? "chat" : "mail";

    rest = rest.substring(index + 3);
    index = rest.indexOf(marker);
  }

  parts.push({
    format: format,
    content: rest
  });

  return parts.map(p =>
    <p data-format={p.format}>
      {p.content}
    </p>
  );
}

export default (props) => {
  const { title, isOfficeInitialized } = props;

  const [loading, setLoading] = useState(false);
  const [query, setQuery] = useState("");
  const [msAccessToken, setMsAccessToken] = useState("");
  const [msEvents, setMsEvents] = useState<Array<CalendarEvent>>(null);
  const [messages, setMessages] = useState([
    {
      role: "system",
      content: "You are an virtual assistant. You help the user named Oliver with his emails. Use a positive, encouraging tone."
    },
    {
      role: "system",
      content: "If you provide the content of a draft email, enclose it with three stars."
    },
    {
      role: "system",
      content: "For emails try not to use the phrase: I hope this email finds you well. Or similar phrases."
    }
  ]);

  // Debug Hook
  useEffect(() => console.table(messages), [messages]);

  useEffect(() => {
    let exited = false;

    console.log("Get content of email.")

    Office.context?.mailbox?.item?.body.getAsync(Office.CoercionType.Text,
      (data: Office.AsyncResult<string>) => {
        console.log("Got content of email.");

        if (exited) return;

        const message = {
          role: "system",
          content: `This is the content of an email Oliver received.\n\n###\n${data.value}\n###`
        }

        setMessages(m => [...m, message]);
      });

    return () => { exited = true; };
  }, []);

  useEffect(() => {
    const x = async () => {
      try {
        const accessToken = await Office.auth.getAccessToken({
          allowSignInPrompt: true,
          allowConsentPrompt: true,
          forMSGraphAccess: true
        });

        setMsAccessToken(accessToken);
      } catch (error) {
        if (![13001, 13002, 13006, 13008, 13010].includes(error.code)) {
          console.log('Fallback to MSAL Auth');
          try {
            const accessToken = await getAccessTokenMSAL();
            setMsAccessToken(accessToken);
          } catch (error) {
            console.error(error);
          }
        }
      }
    };

    x();
  }, []);

  useEffect(() => {
    if (msAccessToken == null || msAccessToken === "") return;

    const x = async () => {
      const client = Client.initWithMiddleware({
        authProvider: {
          getAccessToken: async () => msAccessToken
        },
      });

      const events: Array<CalendarEvent> = [];

      const mapEvents = (events: Array<CalendarReponse>) =>
        events.map(o => ({
          id: o.id,
          subject: o.subject,
          start: moment(o.start.dateTime).local().add("hours", 2).toISOString(true),
          end: moment(o.end.dateTime).local().add("hours", 2).toISOString(true)
        } as CalendarEvent));

      const start = moment(Date()).subtract("days", 1);
      const end = moment(Date()).add("days", 14);

      const response = await client
        .api("/me/calendarview")
        .query(`startdatetime=${start.toISOString()}&enddatetime=${end.toISOString()}`)
        .get() as ODataResponse<CalendarReponse>;

      events.push(...mapEvents(response.value));

      let nextLink = response["@odata.nextLink"];

      while (nextLink != null && nextLink !== "") {
        const response = await client
          .api(nextLink)
          .get() as ODataResponse<CalendarReponse>;

        events.push(...mapEvents(response.value));
        nextLink = response["@odata.nextLink"];
      }

      setMsEvents(events);
    };

    x();
  }, [msAccessToken]);

  useEffect(() => {
    if (msEvents == null) return;

    const header = "subject;startDateTime;endDateTime";
    const body = msEvents.map(o => `${o.subject};${o.start.replace(".000+02:00", "")};${o.end.replace(".000+02:00", "")}`).join("\n");
    const msEventsCsv = `${header}\n${body}`;

    const msEventsMessage = {
      role: "system",
      content: "This is a list of events Oliver has in his calendar." +
        "The format of the list is CSV." + 
        "The first row containes the headers." +
        "Today is the 21.05.2023." +
        "\n\n###\n" + msEventsCsv + "\n###"
    };

    console.log(msEventsMessage.content);

    setMessages(m => [...m, msEventsMessage]);
  }, [msEvents]);

  const onKeyDown = useCallback(async (e: KeyboardEvent) => {
    if (e.code === 'Enter') {
      setLoading(true);

      const requestMessage = {
        role: "user",
        content: query
      };

      setMessages(m => [...m, requestMessage]);

      const response = await queryOpenAi([...messages, requestMessage]);
      const responseMessage = response.choices[0].message;
      setMessages(m => [...m, responseMessage]);

      setQuery("");
      setLoading(false);
    }
  }, [query, messages]);

  const onChange = useCallback((_: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>, newValue?: string) => {
    setQuery(newValue)
  }, [setQuery]);

  if (!isOfficeInitialized) {
    return (
      <Progress
        title={title}
        logo={require("./../../../assets/logo-filled.png")}
        message="Please sideload your addin to see app body."
      />
    );
  }

  return (
    <div>
      <h1>Hi Oliver,</h1>
      <span>I am Lilly, here to help you.</span>
      {msAccessToken == "" ? <p>Authenticating ...</p> : null}
      {msEvents == null ? <p>Loading events ...</p> : null}
      <TextField className="query-input" placeholder="Ask me anything" onKeyDown={onKeyDown} onChange={onChange} value={query}></TextField>
      <div className="message-list">
        {
          messages
            .filter(o => o.role != "system")
            .map(o =>
              <div data-role={o.role} className="message-bubble">
                {formatMessageForDisplay(o.content)}
              </div>
            )
        }

        {
          loading ?
            <div data-role="assistant" className="message-bubble">
              {"Thinking ..."}
            </div>
            : null
        }
      </div>
    </div>
  );
};