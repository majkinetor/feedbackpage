# Introduction #

`Feedback` is an assembly implementing HTML panel that can be used by testers/users to report application issues. It defines Feedback namespace and class `FeedbackPanel` along with several other classes that allow you to use panel in different development scenarios.

# Features #
  * Specify issue description, upload attachment.
  * HTML injection via IHttpMethod allows usage without changing the application code.
  * Programmatic or web.config customization
    * Zero configuration for fast start.
    * Every element of the feedback panel is customizable (title, header, dock, css ...)
  * Ajax like experience
  * Dragable window
  * Write your own code (`FeedbackWriter`) to specify how to store issue or use default ones (File or Email writer)
  * Toggle the feedback panel using web.config for entire site or individual pages.
  * Control Feedback panel via JavaScript bookmarklets.
  * Cross Browser
  * Light weighted - around 1KB of html code is injected on each page, ~5KB javascript is linked to the page, assembly itself is ~35KB.
  * Open source

# Usage #
Reference the assembly in your web.application. You have 2 options:
  * Use `FeedbackModule` that implements `IHttpModule` interface to inject the panel into the HTML page. This method can be used with any type of web applicaton without changing the source code. To use the module register it in web.config in `<system.web>\<httpModules>` section:
`     <add name="FeedbackModule" type="Feedback.FeedbackModule, Feedback, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0b70fe138b18831f"/>`
  * Use `FeedbackPage` as a base class for your web form instead of the `System.Web.UI.Page` class. This method allows you to programatically set up `FeedbackPanel` using `this.Feedback` property.


# Configuration #

## Zero configuration ##
The `FeedbackPage` will use safe defaults if its configuration is not present in the web.config. The `title` will be set to match the name of the web application, it will `dock` to right side of the screen and use `TextWriter` to store issues in the text file in the subfolder "Feedback" under the application root.

## Web.config ##
To fully customize behavior of FeedbackPage, register Feedback configuration handler inside `configSection` then add the Feedback section as a `configuration` element:
```
<configuration>
 <configSections>
  <section  name="Feedback"  type="Feedback.Config, Feedback, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0b70fe138b18831f"/>
 </configSections>
  
  <Feedback>
    ...
  </Feedback>
</configuration>
```

## Feedback Section ##
The following table describes attributes and elements of Feedback section:

| **Name** | **Type** | **Values** | **Meaning** | **Default** |
|:---------|:---------|:-----------|:------------|:------------|
| enabled  | attribute | boolean    | Set to false to disable Feedback for site | `true`      |
| dock     | attribute | LMR TMB    | Describes meta position of X and Y coordinates of panel | `RT`  (right top) |
| prefix   | attribute | string     | Prefix to use for css id's | `Feedback_ `|
| width    | attribute | css style  | Panel width | `350px`     |
|          | | | | |
| title    | element  | html       | Panel title | <Application Name> |
| header   | element  | html       | Panel header |             |
| style    | element  | css        | Panel style | [default style](css_definiton.md)|
| exclude  | element  | string     | Set of URLs to be excluded, each on separate line.|             |
| ext      | element  | string     | Set of file extensions to inject HTML into upon request |`.asp;.aspx;.htm;.html;`|
|writers   | element  | writer     | FeedbackWriter configuration, see below| `TextWriter` |

## Writers ##
FeedbackPage uses `FeedbackWriter`s to handle feedback storage. It implements 2 default writers, TextWriter & EmailWriter for fast start. You can create your own writer by extending FeedbackWriter class and implementing its `WriteIssue` method. You receive instance of the `Input` class, which contains all information about the submitted feedback. The base class will also populate the `args` field, dictionary that contains all xml elements found inside `writer` tag. You can use those elements as arguments for your writer.

Inside `Writers` element you specify set of writers to be called when feedback is submitted. The writer has one mandatory attribute, `type` which you set to the dotNet type implementing your writer, and 1 optional attribute, `enabled` which controls the writer runing state. Elements inside writer tag will be seen as its arguments.

```
<writers>

      <writer type="Feedback.DefaultWriters.EmailWriter">
        <Smtp>mySmtpServerIPorAddress</Smtp>
        <Email>myEmail@mydomain.com, myEmail2@mydomain.com</Email>
      </writer>

      <writer type="Feedback.DefaultWriters.TextWriter">
        <DataFolder>c:\___ISSUES___</DataFolder>
      </writer>

      <writer enabled="false" type="MyApp.MyWriter"/>
 
</writers>
```

In the above xml example, 3 writers are defined. Two are provided by library, one by user (the one that is disabled). Default writers have some parameters provided.

Parameters of default writers are given bellow:

  * **TextWritter**
    * _DataFolder_ - Optional, specify location to keep attachments and textual feedback data. By default, `Application Root\Feedback`
  * **EmailWriter**
    * _Smtp_ - Mandatory, specify smtp server
    * _Email_ - Mandatory, list of emails separated by comma
    * _Sender_ - Optional, by default `Feedback@localhost`
    * _Timeout_ - Optional, timeout in seconds for sending email, by default 20s.

[Web.config Sample](Web_config.md)

### Programatically ###
You can set-up FeedbackPage within your code. For instance, you may want to change the dock type on specific pages or enable the Feedback panel only for users that logged in with testing privileges. You can access the panel using `this.Feedback` property.

For instance, the following code changes the title of the Feedback to match the logged user name:
```
    public partial class _Default : Feedback.FeedbackPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            this.Feedback.Title = "Feedback for " + loggedUser.Name;
        }
    }
```
In the browser, you can control Feedback panel via JavaScript [bookmarklets](http://en.wikipedia.org/wiki/Bookmarklet).
  * `Feedback.show()`   - Show the feedback panel
  * `Feedback.hide()`   - Close the feedback panel
  * `Feedback.toggle()` - Toggle the minimize/maximize state

For fastest usage, add above functions as bookmarks in your browser.

## Source Code ##
The repository contains Visual Studio 2008 solution that contains 3 projects: class library (Feedback assembly) and two test applications (MVC and Web Forms). It is build against framework version 3.5.

## Screenshot ##
First screenshot shows maximized `FeedbackPage` in IE7, with default style, docked to the right side of the page (right top - RT). Second screenshot shows minimized `FeedbackModule`, docked to the bottom side of the page (dock equals to left bottom - LB) , running in default MVC application.

|![http://feedbackpage.googlecode.com/svn/trunk/Feedback/webapp.png](http://feedbackpage.googlecode.com/svn/trunk/Feedback/webapp.png)|![http://feedbackpage.googlecode.com/svn/trunk/Feedback/mvc.png](http://feedbackpage.googlecode.com/svn/trunk/Feedback/mvc.png)|
|:------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------|