import { AppContainer } from "react-hot-loader";
import { initializeIcons } from "@fluentui/font-icons-mdl2";
import { ThemeProvider } from "@fluentui/react";
import * as React from "react";
import * as ReactDOM from "react-dom";
import "../auth/authRedirect";

/* global document, Office, module, require */

initializeIcons();

let isOfficeInitialized = false;

const render = () => {
  ReactDOM.render(
    <AppContainer>
      <ThemeProvider>

      </ThemeProvider>
    </AppContainer>,
    document.getElementById("container")
  );
};

/* Render application after Office initializes */
Office.onReady(() => {
  isOfficeInitialized = true;
  render();
});
