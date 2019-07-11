using System;
using System.Collections.Generic;
using ConnectApp.Components;
using ConnectApp.Constants;
using ConnectApp.Models.ActionModel;
using ConnectApp.Models.Model;
using ConnectApp.Models.State;
using ConnectApp.Models.ViewModel;
using ConnectApp.redux.actions;
using RSG;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.Redux;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.scheduler;
using Unity.UIWidgets.service;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;

namespace ConnectApp.screens {
    public class SearchScreenConnector : StatelessWidget {
        public SearchScreenConnector(
            Key key = null
        ) : base(key: key) { 
        }

        public override Widget build(BuildContext context) {
            return new StoreConnector<AppState, SearchScreenViewModel>(
                converter: state => new SearchScreenViewModel {
                    searchKeyword = state.searchState.keyword,
                    searchArticles = state.searchState.searchArticles.ContainsKey(key: state.searchState.keyword)
                        ? state.searchState.searchArticles[key: state.searchState.keyword]
                        : new List<Article>(),
                    searchArticleHistoryList = state.searchState.searchArticleHistoryList,
                    popularSearchArticleList = state.popularSearchState.popularSearchArticles,
                    searchUsers = state.searchState.searchUsers.ContainsKey(key: state.searchState.keyword)
                        ? state.searchState.searchUsers[key: state.searchState.keyword]
                        : new List<User>()
                },
                builder: (context1, viewModel, dispatcher) => {
                    var actionModel = new SearchScreenActionModel {
                        mainRouterPop = () => dispatcher.dispatch(new MainNavigatorPopAction()),
                        fetchPopularSearch = () => dispatcher.dispatch<IPromise>(Actions.popularSearchArticle()),
                        startSearchArticle = keyword => dispatcher.dispatch(new StartSearchArticleAction {
                            keyword = keyword
                        }),
                        searchArticle = (keyword, pageNumber) => dispatcher.dispatch<IPromise>(
                            Actions.searchArticles(keyword, pageNumber)),
                        startSearchUser = () => dispatcher.dispatch(new StartSearchUserAction()),
                        searchUser = (keyword, pageNumber) => dispatcher.dispatch<IPromise>(
                            Actions.searchUsers(keyword, pageNumber)),
                        clearSearchResult = () => dispatcher.dispatch(new ClearSearchResultAction()),
                        saveSearchArticleHistory = keyword =>
                            dispatcher.dispatch(new SaveSearchArticleHistoryAction {keyword = keyword}),
                        deleteSearchArticleHistory = keyword => 
                            dispatcher.dispatch(new DeleteSearchArticleHistoryAction {keyword = keyword}),
                        deleteAllSearchArticleHistory = () =>
                            dispatcher.dispatch(new DeleteAllSearchArticleHistoryAction())
                    };
                    return new SearchScreen(viewModel, actionModel);
                }
            );
        }
    }

    public class SearchScreen : StatefulWidget {
        public SearchScreen(
            SearchScreenViewModel viewModel = null,
            SearchScreenActionModel actionModel = null,
            Key key = null
        ) : base(key: key) {
            this.viewModel = viewModel;
            this.actionModel = actionModel;
        }

        public readonly SearchScreenViewModel viewModel;
        public readonly SearchScreenActionModel actionModel;

        public override State createState() {
            return new _SearchScreenState();
        }
    }

    class _SearchScreenState : State<SearchScreen> {
        readonly TextEditingController _controller = new TextEditingController("");
        FocusNode _focusNode;
        PageController _pageController;
        int _selectedIndex;

        public override void initState() {
            base.initState();
            this._focusNode = new FocusNode();
            this._pageController = new PageController();
            this._selectedIndex = 0;
            SchedulerBinding.instance.addPostFrameCallback(_ => {
                if (this.widget.viewModel.searchKeyword.Length > 0
                    || this.widget.viewModel.searchArticles.Count > 0
                    || this.widget.viewModel.searchUsers.Count > 0) {
                    this.widget.actionModel.clearSearchResult();
                }

                this.widget.actionModel.fetchPopularSearch();
            });
        }

        public override void dispose() {
            this._controller.dispose();
            this._pageController.dispose();
            base.dispose();
        }

        void _searchResult(string text) {
            if (text.isEmpty()) {
                return;
            }

            if (this._focusNode.hasFocus) {
                this._focusNode.unfocus();
            }

            this._controller.text = text;

            if (this._selectedIndex == 0) {
                this._searchArticle(text: text);
            }
            if (this._selectedIndex == 1) {
                this._searchUser(text: text);
            }
        }

        void _searchArticle(string text) {
            this.widget.actionModel.saveSearchArticleHistory(text);
            this.widget.actionModel.startSearchArticle(text);
            this.widget.actionModel.searchArticle(text, 0);
        }
        
        void _searchUser(string text) {
            this.widget.actionModel.startSearchUser();
            this.widget.actionModel.searchUser(text, 1);
        }

        public override Widget build(BuildContext context) {
            Widget child = new Container();
            if (this.widget.viewModel.searchKeyword.Length > 0) {
                child = this._buildSearchResult();
            }
            else {
                child = new ListView(
                    children: new List<Widget> {
                        this._buildSearchHistory(),
                        this._buildPopularSearch()
                    }
                );
            }

            return new Container(
                color: CColors.White,
                child: new CustomSafeArea(
                    child: new Container(
                        child: new Column(
                            children: new List<Widget> {
                                this._buildSearchBar(),
                                new Flexible(
                                    child: new NotificationListener<ScrollNotification>(
                                        onNotification: notification => {
                                            if (this._focusNode.hasFocus) {
                                                this._focusNode.unfocus();
                                            }
                                            return true;
                                        },
                                        child: child
                                    )
                                )
                            }
                        )
                    )
                )
            );
        }

        Widget _buildSearchBar() {
            return new Container(
                height: 94,
                padding: EdgeInsets.only(16, 0, 16, 12),
                color: CColors.White,
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.end,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: new List<Widget> {
                        new CustomButton(
                            padding: EdgeInsets.only(8, 8, 0, 8),
                            onPressed: () => this.widget.actionModel.mainRouterPop(),
                            child: new Text(
                                "取消",
                                style: CTextStyle.PLargeBlue
                            )
                        ),
                        new InputField(
                            height: 40,
                            controller: this._controller,
                            focusNode: this._focusNode,
                            style: CTextStyle.H2,
                            autofocus: true,
                            hintText: "搜索",
                            hintStyle: CTextStyle.H2Body4,
                            cursorColor: CColors.PrimaryBlue,
                            textInputAction: TextInputAction.search,
                            clearButtonMode: InputFieldClearButtonMode.whileEditing,
                            onChanged: text => {
                                if (text == null || text.Length <= 0) {
                                    this._selectedIndex = 0;
                                    this.widget.actionModel.clearSearchResult();
                                }
                            },
                            onSubmitted: this._searchResult
                        )
                    }
                )
            );
        }

        Widget _buildSearchResult() {
            return new Container(
                child: new Column(
                    children: new List<Widget> {
                        this._buildSelectView(),
                        this._buildContentView()
                    }
                )
            );
        }

        Widget _buildSelectView() {
            return new CustomSegmentedControl(
                new List<string> {"文章", "用户"},
                newValue => {
                    this.setState(() => this._selectedIndex = newValue);
                    this._pageController.animateToPage(
                        page: newValue,
                        TimeSpan.FromMilliseconds(250),
                        curve: Curves.ease
                    );
                },
                currentIndex: this._selectedIndex
            );
        }

        Widget _buildContentView() {
            return new Flexible(
                child: new Container(
                    child: new PageView(
                        physics: new BouncingScrollPhysics(),
                        controller: this._pageController,
                        onPageChanged: index => {
                            this.setState(() => this._selectedIndex = index);
                            this._searchResult(this.widget.viewModel.searchKeyword);
                        },
                        children: new List<Widget> {
                            new SearchArticleScreenConnector(),
                            new SearchUserScreenConnector()
                        }
                    )
                )
            );
        }

        Widget _buildPopularSearch() {
            if (this.widget.viewModel.popularSearchArticleList.Count <= 0) {
                return new Container();
            }

            return new Container(
                padding: EdgeInsets.only(16, 24, 16),
                color: CColors.White,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: new List<Widget> {
                        new Container(
                            margin: EdgeInsets.only(bottom: 16),
                            child: new Text(
                                "热门搜索",
                                style: CTextStyle.PXLargeBody4
                            )
                        ),
                        new Wrap(
                            spacing: 8,
                            runSpacing: 20,
                            children: this._buildPopularSearchItem()
                        )
                    }
                )
            );
        }

        List<Widget> _buildPopularSearchItem() {
            var popularSearch = this.widget.viewModel.popularSearchArticleList;
            List<Widget> widgets = new List<Widget>();
            popularSearch.ForEach(item => {
                Widget widget = new GestureDetector(
                    onTap: () => this._searchResult(item.keyword),
                    child: new Container(
                        decoration: new BoxDecoration(
                            CColors.Separator2,
                            borderRadius: BorderRadius.circular(16)
                        ),
                        height: 32,
                        padding: EdgeInsets.only(16, 7, 16),
                        child: new Text(
                            item.keyword,
                            maxLines: 1,
                            style: new TextStyle(
                                fontSize: 16,
                                fontFamily: "Roboto-Regular",
                                color: CColors.TextBody
                            ),
                            overflow: TextOverflow.ellipsis
                        )
                    )
                );
                widgets.Add(widget);
            });
            return widgets;
        }

        Widget _buildSearchHistory() {
            var searchHistoryList = this.widget.viewModel.searchArticleHistoryList;
            if (searchHistoryList == null || searchHistoryList.Count <= 0) {
                return new Container();
            }

            var widgets = new List<Widget> {
                new Container(
                    margin: EdgeInsets.only(top: 24, bottom: 10),
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: new List<Widget> {
                            new Text(
                                "搜索历史",
                                style: CTextStyle.PXLargeBody4
                            ),
                            new CustomButton(
                                padding: EdgeInsets.only(8, 8, 0, 8),
                                onPressed: () => {
                                    ActionSheetUtils.showModalActionSheet(
                                        new ActionSheet(
                                            title: "确定清除搜索历史记录？",
                                            items: new List<ActionSheetItem> {
                                                new ActionSheetItem("确定", ActionType.destructive,
                                                    () => this.widget.actionModel.deleteAllSearchArticleHistory()),
                                                new ActionSheetItem("取消", ActionType.cancel)
                                            }
                                        )
                                    );
                                },
                                child: new Text(
                                    "清空",
                                    style: CTextStyle.PRegularBody4
                                )
                            )
                        }
                    )
                )
            };
            searchHistoryList.ForEach(item => {
                var child = new GestureDetector(
                    onTap: () => this._searchResult(item),
                    child: new Container(
                        height: 44,
                        color: CColors.White,
                        child: new Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: new List<Widget> {
                                new Expanded(
                                    child: new Text(
                                        data: item,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: CTextStyle.PLargeBody
                                    )
                                ),
                                new CustomButton(
                                    padding: EdgeInsets.only(8, 8, 0, 8),
                                    onPressed: () => this.widget.actionModel.deleteSearchArticleHistory(item),
                                    child: new Icon(
                                        Icons.close,
                                        size: 16,
                                        color: Color.fromRGBO(199, 203, 207, 1)
                                    )
                                )
                            }
                        )
                    )
                );
                widgets.Add(child);
            });

            return new Container(
                padding: EdgeInsets.symmetric(horizontal: 16),
                color: CColors.White,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: widgets
                )
            );
        }
    }
}