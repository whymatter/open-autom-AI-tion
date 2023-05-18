import * as React from "react";
import { DefaultButton, TextField } from "@fluentui/react";
import Progress from "./Progress";
import { useCallback, useEffect } from "react";
import { KeyboardEvent } from "react";
import { useState } from "react";
import config from "../../config/config";

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
  const [messages, setMessages] = useState([
    {
      role: "system",
      content: "You are an virtual assistant. You help the user named Oliver with his emails. Use a positive, encouraging tone."
    },
    {
      role: "system",
      content: "If you provide the content of a draft email, enclose it with three stars."
    }
  ]);

  // Debug Hook
  useEffect(() => console.table(messages), [messages]);

  useEffect(() => {
    let exited = false;

    console.log("Get content of email.")

    Office.context.mailbox.item?.body.getAsync(Office.CoercionType.Text,
      (data: Office.AsyncResult<string>) => {
        console.log("Got content of email.");

        if (exited) return;

        const message = {
          role: "system",
          content: `The current email he needs help with has the following content.\n\n###\n${data.value}\n###`
        }

        setMessages(m => [...m, message]);
      });

    return () => { exited = true; };
  }, []);

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